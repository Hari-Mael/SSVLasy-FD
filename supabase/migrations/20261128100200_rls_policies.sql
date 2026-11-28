create or replace function private.my_role()
returns public.user_role
language sql
stable
security definer
set search_path = ''
as $$
    select p.role from public.profiles p where p.id = auth.uid() and p.active
$$;

create or replace function private.my_team_id()
returns uuid
language sql
stable
security definer
set search_path = ''
as $$
    select tm.team_id from public.team_members tm where tm.user_id = auth.uid()
$$;

create or replace function private.my_chef_id()
returns uuid
language sql
stable
security definer
set search_path = ''
as $$
    select t.chef_id
    from public.team_members tm
    join public.teams t on t.id = tm.team_id
    where tm.user_id = auth.uid()
$$;

create or replace function private.is_super_admin()
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
    select coalesce(private.my_role() = 'super_admin', false)
$$;

create or replace function private.can_manage_chef(chef uuid)
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
    select private.is_super_admin() or (private.my_role() = 'chef' and auth.uid() = chef)
$$;

create or replace function private.can_manage_team(team uuid)
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
    select exists (
        select 1 from public.teams t where t.id = team and private.can_manage_chef(t.chef_id)
    )
$$;

create or replace function private.can_manage_campaign(campaign uuid)
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
    select exists (
        select 1 from public.campaigns c
        where c.id = campaign and private.can_manage_chef(c.owner_id)
    )
$$;

create or replace function private.can_manage_activity(activity uuid)
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
    select exists (
        select 1
        from public.activities a
        join public.campaigns c on c.id = a.campaign_id
        where a.id = activity and private.can_manage_chef(c.owner_id)
    )
$$;

alter table public.game_config enable row level security;
alter table public.profiles enable row level security;
alter table public.teams enable row level security;
alter table public.team_members enable row level security;
alter table public.member_guardians enable row level security;
alter table public.campaigns enable row level security;
alter table public.activities enable row level security;
alter table public.activity_secrets enable row level security;
alter table public.team_activity enable row level security;
alter table public.attempts enable row level security;
alter table public.team_camp_state enable row level security;
alter table public.team_day_actions enable row level security;

create policy game_config_read on public.game_config
    for select to authenticated using (true);

create policy profiles_select on public.profiles
    for select to authenticated using (
        id = auth.uid()
        or private.is_super_admin()
        or exists (
            select 1
            from public.team_members tm
            join public.teams t on t.id = tm.team_id
            where tm.user_id = profiles.id and t.chef_id = auth.uid()
        )
    );

create policy teams_select on public.teams
    for select to authenticated using (
        private.can_manage_chef(chef_id) or id = private.my_team_id()
    );
create policy teams_manage on public.teams
    for all to authenticated
    using (private.can_manage_chef(chef_id))
    with check (private.can_manage_chef(chef_id));

create policy team_members_select on public.team_members
    for select to authenticated using (
        user_id = auth.uid()
        or team_id = private.my_team_id()
        or private.can_manage_team(team_id)
    );
create policy team_members_manage on public.team_members
    for all to authenticated
    using (private.can_manage_team(team_id))
    with check (private.can_manage_team(team_id));

create policy member_guardians_manage on public.member_guardians
    for all to authenticated
    using (exists (
        select 1 from public.team_members tm
        where tm.user_id = member_guardians.user_id and private.can_manage_team(tm.team_id)
    ))
    with check (exists (
        select 1 from public.team_members tm
        where tm.user_id = member_guardians.user_id and private.can_manage_team(tm.team_id)
    ));

create policy campaigns_select on public.campaigns
    for select to authenticated using (
        private.can_manage_chef(owner_id) or owner_id = private.my_chef_id()
    );
create policy campaigns_manage on public.campaigns
    for all to authenticated
    using (private.can_manage_chef(owner_id))
    with check (private.can_manage_chef(owner_id));

create policy activities_select on public.activities
    for select to authenticated using (
        private.can_manage_campaign(campaign_id)
        or (status <> 'draft' and exists (
            select 1 from public.campaigns c
            where c.id = activities.campaign_id and c.owner_id = private.my_chef_id()
        ))
    );
create policy activities_manage on public.activities
    for all to authenticated
    using (private.can_manage_campaign(campaign_id))
    with check (private.can_manage_campaign(campaign_id));

create policy activity_secrets_manage on public.activity_secrets
    for all to authenticated
    using (private.can_manage_activity(activity_id))
    with check (private.can_manage_activity(activity_id));

create policy team_activity_select on public.team_activity
    for select to authenticated using (
        team_id = private.my_team_id() or private.can_manage_team(team_id)
    );

create policy attempts_select on public.attempts
    for select to authenticated using (
        team_id = private.my_team_id() or private.can_manage_team(team_id)
    );

create policy team_camp_state_select on public.team_camp_state
    for select to authenticated using (
        team_id = private.my_team_id() or private.can_manage_team(team_id)
    );

create policy team_day_actions_select on public.team_day_actions
    for select to authenticated using (
        team_id = private.my_team_id() or private.can_manage_team(team_id)
    );

revoke all on all tables in schema public from anon, authenticated;
grant select on public.game_config, public.profiles, public.teams, public.team_members,
    public.member_guardians, public.campaigns, public.activities, public.activity_secrets,
    public.team_activity, public.attempts, public.team_camp_state, public.team_day_actions
    to authenticated;
grant insert, update, delete on public.teams, public.team_members, public.member_guardians,
    public.campaigns, public.activities, public.activity_secrets
    to authenticated;

alter default privileges in schema public revoke all on tables from anon, authenticated;

revoke all on schema private from public;
grant usage on schema private to authenticated;
revoke execute on all functions in schema private from public;
grant execute on all functions in schema private to authenticated;
