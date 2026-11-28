create schema if not exists private;

create or replace function private.enforce_profile_limits()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
    current_count integer;
    limit_value integer;
begin
    if new.role = 'member' then
        return new;
    end if;
    if tg_op = 'UPDATE' and old.role = new.role then
        return new;
    end if;

    perform pg_advisory_xact_lock(hashtext('profiles_role_' || new.role::text));
    select count(*) into current_count
    from public.profiles
    where role = new.role and id <> new.id;

    select case when new.role = 'super_admin' then max_super_admins else max_chefs end
    into limit_value
    from public.game_config;

    if current_count >= limit_value then
        raise exception 'QUOTA_USERS_%', upper(new.role::text) using errcode = 'P0001';
    end if;
    return new;
end;
$$;

create trigger profiles_role_quota
before insert or update of role on public.profiles
for each row execute function private.enforce_profile_limits();

create or replace function private.guard_last_super_admin()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
    if old.role = 'super_admin' and old.active
       and (tg_op = 'DELETE' or new.role <> 'super_admin' or not new.active)
       and not exists (
           select 1 from public.profiles
           where role = 'super_admin' and active and id <> old.id
       ) then
        raise exception 'LAST_SUPER_ADMIN' using errcode = 'P0001';
    end if;
    if tg_op = 'DELETE' then
        return old;
    end if;
    return new;
end;
$$;

create trigger profiles_keep_super_admin
before delete or update of role, active on public.profiles
for each row execute function private.guard_last_super_admin();

create or replace function private.enforce_team_owner_and_quota()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
    current_count integer;
    limit_value integer;
begin
    if not exists (
        select 1 from public.profiles
        where id = new.chef_id and role = 'chef' and active
    ) then
        raise exception 'OWNER_NOT_CHEF' using errcode = 'P0001';
    end if;

    perform pg_advisory_xact_lock(hashtext('team_quota_' || new.chef_id::text));
    select count(*) into current_count
    from public.teams
    where chef_id = new.chef_id and id <> new.id;

    select max_teams_per_chef into limit_value from public.game_config;
    if current_count >= limit_value then
        raise exception 'QUOTA_TEAMS_PER_CHEF' using errcode = 'P0001';
    end if;
    return new;
end;
$$;

create trigger teams_owner_and_quota
before insert or update of chef_id on public.teams
for each row execute function private.enforce_team_owner_and_quota();

create or replace function private.enforce_team_member_limit()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
    current_count integer;
    limit_value integer;
begin
    if not exists (
        select 1 from public.profiles
        where id = new.user_id and role = 'member' and active
    ) then
        raise exception 'NOT_ACTIVE_MEMBER' using errcode = 'P0001';
    end if;

    perform pg_advisory_xact_lock(hashtext('member_quota_' || new.team_id::text));
    select count(*) into current_count
    from public.team_members
    where team_id = new.team_id and user_id <> new.user_id;

    select max_members_per_team into limit_value from public.game_config;
    if current_count >= limit_value then
        raise exception 'QUOTA_MEMBERS_PER_TEAM' using errcode = 'P0001';
    end if;
    return new;
end;
$$;

create trigger team_members_guard
before insert or update of user_id, team_id on public.team_members
for each row execute function private.enforce_team_member_limit();

create or replace function private.enforce_campaign_owner()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
    if not exists (
        select 1 from public.profiles
        where id = new.owner_id and role = 'chef' and active
    ) then
        raise exception 'OWNER_NOT_CHEF' using errcode = 'P0001';
    end if;
    return new;
end;
$$;

create trigger campaigns_owner_is_chef
before insert or update of owner_id on public.campaigns
for each row execute function private.enforce_campaign_owner();

create or replace function private.enforce_activity_slot()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
    current_count integer;
    campaign_days smallint;
    limit_value integer;
begin
    select days into campaign_days from public.campaigns where id = new.campaign_id;
    if campaign_days is null or new.day > campaign_days then
        raise exception 'DAY_OUT_OF_CAMPAIGN' using errcode = 'P0001';
    end if;

    perform pg_advisory_xact_lock(
        hashtext('activities_day_' || new.campaign_id::text || '_' || new.day::text)
    );
    select count(*) into current_count
    from public.activities
    where campaign_id = new.campaign_id and day = new.day and id <> new.id;

    select max_activities_per_day into limit_value from public.game_config;
    if current_count >= limit_value then
        raise exception 'QUOTA_ACTIVITIES_PER_DAY' using errcode = 'P0001';
    end if;
    return new;
end;
$$;

create trigger activities_slot_guard
before insert or update of campaign_id, day on public.activities
for each row execute function private.enforce_activity_slot();

create or replace function private.require_member_guardian()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
    target uuid;
begin
    if tg_table_name = 'team_members' then
        target := new.user_id;
    else
        target := old.user_id;
    end if;
    if exists (select 1 from public.team_members where user_id = target)
       and not exists (select 1 from public.member_guardians where user_id = target) then
        raise exception 'GUARDIAN_REQUIRED' using errcode = 'P0001';
    end if;
    return null;
end;
$$;

create constraint trigger team_members_need_guardian
after insert or update of user_id on public.team_members
deferrable initially deferred
for each row execute function private.require_member_guardian();

create constraint trigger member_guardians_keep_guardian
after delete on public.member_guardians
deferrable initially deferred
for each row execute function private.require_member_guardian();
