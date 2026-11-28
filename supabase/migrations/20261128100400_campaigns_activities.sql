alter table public.game_config
    add column min_chefs_to_schedule integer not null default 6 check (min_chefs_to_schedule > 0);

create or replace function public.create_campaign(
    p_name text,
    p_camp_type public.camp_type,
    p_days integer,
    p_start_date date,
    p_timezone text default 'Indian/Antananarivo'
)
returns uuid
language plpgsql
security definer
set search_path = ''
as $$
declare
    new_campaign_id uuid;
begin
    if private.my_role() is distinct from 'chef' then
        raise exception 'REAL_CHEF_REQUIRED' using errcode = '42501';
    end if;
    if p_days < 1 or p_days > 11
       or (p_camp_type = 'tselatra' and p_days > 3)
       or (p_camp_type = 'lehibe' and p_days > 5) then
        raise exception 'DUREE_INVALIDE' using errcode = 'P0001';
    end if;
    if not exists (select 1 from pg_timezone_names where name = p_timezone) then
        raise exception 'FUSEAU_INCONNU' using errcode = '22023';
    end if;

    insert into public.campaigns (owner_id, name, camp_type, days, start_date, timezone)
    values (auth.uid(), trim(p_name), p_camp_type, p_days::smallint, p_start_date, p_timezone)
    returning id into new_campaign_id;

    return new_campaign_id;
end;
$$;

create or replace function public.transition_campaign(
    p_campaign_id uuid,
    p_target public.campaign_state
)
returns public.campaign_state
language plpgsql
security definer
set search_path = ''
as $$
declare
    v_campaign public.campaigns%rowtype;
    v_active_chefs integer;
    v_minimum_chefs integer;
    v_allowed boolean;
begin
    if not private.can_manage_campaign(p_campaign_id) then
        raise exception 'NON_AUTORISE' using errcode = '42501';
    end if;

    select * into v_campaign from public.campaigns where id = p_campaign_id for update;

    v_allowed := case v_campaign.state
        when 'draft' then p_target = 'scheduled'
        when 'scheduled' then p_target = 'running'
        when 'running' then p_target in ('paused', 'finished')
        when 'paused' then p_target in ('running', 'finished')
        else false
    end;
    if not v_allowed then
        raise exception 'TRANSITION_CAMPAGNE' using errcode = 'P0001';
    end if;

    if p_target = 'scheduled' then
        select min_chefs_to_schedule into v_minimum_chefs from public.game_config;
        select count(*) into v_active_chefs from public.profiles where role = 'chef' and active;
        if v_active_chefs < v_minimum_chefs then
            raise exception 'CHEFS_INSUFFICIENTS' using errcode = 'P0001';
        end if;
        if v_campaign.start_date < (now() at time zone v_campaign.timezone)::date then
            raise exception 'DATE_DEBUT_PASSEE' using errcode = 'P0001';
        end if;
    end if;

    update public.campaigns set state = p_target where id = p_campaign_id;
    return p_target;
end;
$$;

create or replace function public.insert_activity(
    p_activity_id uuid,
    p_campaign_id uuid,
    p_title text,
    p_day integer,
    p_slot_start time,
    p_slot_end time,
    p_points integer,
    p_cipher public.cipher_kind,
    p_encoded_text text,
    p_answer_digest text,
    p_solution_sealed bytea,
    p_cipher_shift integer,
    p_cipher_key text
)
returns uuid
language plpgsql
security definer
set search_path = ''
as $$
declare
    v_state public.campaign_state;
begin
    select state into v_state from public.campaigns where id = p_campaign_id for update;
    if v_state is null then
        raise exception 'CAMPAGNE_INCONNUE' using errcode = 'P0001';
    end if;
    if v_state = 'finished' then
        raise exception 'CAMPAGNE_TERMINEE' using errcode = 'P0001';
    end if;

    insert into public.activities (
        id, campaign_id, title, day, slot_start, slot_end, points, cipher, encoded_text
    )
    values (
        p_activity_id, p_campaign_id, trim(p_title), p_day::smallint, p_slot_start, p_slot_end,
        p_points, p_cipher, p_encoded_text
    );

    insert into public.activity_secrets (
        activity_id, cipher_shift, cipher_key, answer_digest, solution_sealed
    )
    values (
        p_activity_id, p_cipher_shift::smallint, p_cipher_key, p_answer_digest::char(64),
        p_solution_sealed
    );

    return p_activity_id;
end;
$$;

create or replace function public.publish_activity(p_activity_id uuid)
returns void
language plpgsql
security definer
set search_path = ''
as $$
declare
    v_status public.activity_status;
begin
    if not private.can_manage_activity(p_activity_id) then
        raise exception 'NON_AUTORISE' using errcode = '42501';
    end if;

    select status into v_status from public.activities where id = p_activity_id for update;
    if v_status is distinct from 'draft' then
        raise exception 'TRANSITION_ACTIVITE' using errcode = 'P0001';
    end if;

    update public.activities set status = 'published' where id = p_activity_id;
end;
$$;

create or replace function public.unlock_activity(p_activity_id uuid)
returns void
language plpgsql
security definer
set search_path = ''
as $$
declare
    v_status public.activity_status;
    v_campaign_state public.campaign_state;
begin
    if not private.can_manage_activity(p_activity_id) then
        raise exception 'NON_AUTORISE' using errcode = '42501';
    end if;

    select a.status, c.state into v_status, v_campaign_state
    from public.activities a
    join public.campaigns c on c.id = a.campaign_id
    where a.id = p_activity_id
    for update of a;

    if v_status is distinct from 'published' then
        raise exception 'TRANSITION_ACTIVITE' using errcode = 'P0001';
    end if;
    if v_campaign_state is distinct from 'running' then
        raise exception 'CAMPAGNE_NON_EN_COURS' using errcode = 'P0001';
    end if;

    update public.activities set status = 'unlocked' where id = p_activity_id;
end;
$$;

drop policy campaigns_manage on public.campaigns;
drop policy activities_manage on public.activities;
drop policy activity_secrets_manage on public.activity_secrets;

create policy activity_secrets_select on public.activity_secrets
    for select to authenticated using (private.can_manage_activity(activity_id));

revoke insert, update, delete on public.campaigns, public.activities, public.activity_secrets
    from authenticated;

revoke execute on function public.create_campaign(text, public.camp_type, integer, date, text)
    from public, anon;
grant execute on function public.create_campaign(text, public.camp_type, integer, date, text)
    to authenticated;

revoke execute on function public.transition_campaign(uuid, public.campaign_state)
    from public, anon;
grant execute on function public.transition_campaign(uuid, public.campaign_state)
    to authenticated;

revoke execute on function public.publish_activity(uuid) from public, anon;
grant execute on function public.publish_activity(uuid) to authenticated;

revoke execute on function public.unlock_activity(uuid) from public, anon;
grant execute on function public.unlock_activity(uuid) to authenticated;

revoke execute on function public.insert_activity(
    uuid, uuid, text, integer, time, time, integer, public.cipher_kind, text, text, bytea, integer, text
) from public, anon, authenticated;
grant execute on function public.insert_activity(
    uuid, uuid, text, integer, time, time, integer, public.cipher_kind, text, text, bytea, integer, text
) to service_role;
