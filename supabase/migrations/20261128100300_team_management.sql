revoke insert, update, delete on public.teams, public.team_members, public.member_guardians from authenticated;

drop policy teams_manage on public.teams;
drop policy team_members_manage on public.team_members;
drop policy member_guardians_manage on public.member_guardians;

create or replace function public.create_team(p_name text)
returns uuid
language plpgsql
security definer
set search_path = ''
as $$
declare
    new_team_id uuid;
begin
    if private.my_role() is distinct from 'chef' then
        raise exception 'REAL_CHEF_REQUIRED' using errcode = '42501';
    end if;

    insert into public.teams (chef_id, name)
    values (auth.uid(), trim(p_name))
    returning id into new_team_id;

    return new_team_id;
end;
$$;

create or replace function public.register_member(
    p_chef_id uuid,
    p_team_id uuid,
    p_user_id uuid,
    p_login text,
    p_display_name text,
    p_parent_name text,
    p_parent_contact text
)
returns void
language plpgsql
security definer
set search_path = ''
as $$
begin
    if not exists (
        select 1 from public.teams where id = p_team_id and chef_id = p_chef_id
    ) then
        raise exception 'TEAM_NOT_OWNED' using errcode = 'P0001';
    end if;

    insert into public.profiles (id, role, login, display_name)
    values (p_user_id, 'member', p_login, p_display_name);

    insert into public.team_members (user_id, team_id)
    values (p_user_id, p_team_id);

    insert into public.member_guardians (user_id, parent_name, parent_contact)
    values (p_user_id, p_parent_name, p_parent_contact);
end;
$$;

revoke execute on function public.create_team(text) from public, anon;
grant execute on function public.create_team(text) to authenticated;

revoke execute on function public.register_member(uuid, uuid, uuid, text, text, text, text)
    from public, anon, authenticated;
grant execute on function public.register_member(uuid, uuid, uuid, text, text, text, text)
    to service_role;

create policy member_guardians_select on public.member_guardians
    for select to authenticated using (
        exists (
            select 1 from public.team_members tm
            where tm.user_id = member_guardians.user_id and private.can_manage_team(tm.team_id)
        )
    );
