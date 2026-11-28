create extension if not exists postgis with schema extensions;

create type public.user_role as enum ('super_admin', 'chef', 'member');
create type public.camp_type as enum ('tselatra', 'lehibe', 'feno');
create type public.campaign_state as enum ('draft', 'scheduled', 'running', 'paused', 'finished');
create type public.weather_kind as enum (
    'chaleur', 'pluvieux', 'venteux', 'chaud_humide', 'venteux_humide', 'chaud_sec'
);
create type public.cipher_kind as enum (
    'morse', 'caesar', 'atbash', 'vigenere', 'polybius', 'a1z26', 'reversed',
    'morse_lampe', 'morse_sifflet', 'otan', 'semaphore', 'montre_murale',
    'pyramide', 'code_q', 'chiffre', 'lohateny', 'code_code'
);
create type public.activity_status as enum ('draft', 'published', 'unlocked');
create type public.progress_status as enum (
    'open', 'awaiting_choice', 'free_retry', 'skip_only', 'solved', 'skipped'
);
create type public.attempt_outcome as enum ('correct', 'wrong', 'expired');
create type public.camp_resource as enum ('wood', 'water', 'food');

create table public.game_config (
    id boolean primary key default true check (id),
    max_super_admins integer not null default 10 check (max_super_admins > 0),
    max_chefs integer not null default 100 check (max_chefs > 0),
    max_teams_per_chef integer not null default 3 check (max_teams_per_chef > 0),
    max_members_per_team integer not null default 5 check (max_members_per_team > 0),
    max_activities_per_day integer not null default 7 check (max_activities_per_day > 0)
);
insert into public.game_config (id) values (true);

create table public.profiles (
    id uuid primary key references auth.users (id) on delete cascade,
    role public.user_role not null,
    login text unique check (login ~ '^[a-z0-9-]{1,40}$'),
    display_name text not null check (char_length(display_name) between 1 and 40),
    active boolean not null default true,
    created_at timestamptz not null default now(),
    check (role <> 'member' or login is not null)
);

create table public.teams (
    id uuid primary key default gen_random_uuid(),
    chef_id uuid not null references public.profiles (id) on delete cascade,
    name text not null check (char_length(name) between 1 and 60),
    created_at timestamptz not null default now(),
    unique (chef_id, name)
);

create table public.team_members (
    user_id uuid primary key references public.profiles (id) on delete cascade,
    team_id uuid not null references public.teams (id) on delete cascade,
    energy smallint not null default 100 check (energy between 0 and 100),
    created_at timestamptz not null default now()
);
create index team_members_team_idx on public.team_members (team_id);

create table public.member_guardians (
    user_id uuid primary key references public.team_members (user_id) on delete cascade,
    parent_name text not null check (char_length(trim(parent_name)) > 0),
    parent_contact text not null check (char_length(trim(parent_contact)) > 0)
);

create table public.campaigns (
    id uuid primary key default gen_random_uuid(),
    owner_id uuid not null references public.profiles (id) on delete cascade,
    name text not null check (char_length(name) between 1 and 120),
    camp_type public.camp_type not null default 'tselatra',
    days smallint not null check (days between 1 and 11),
    start_date date not null,
    timezone text not null default 'Indian/Antananarivo',
    state public.campaign_state not null default 'draft',
    current_day smallint not null default 1,
    weather public.weather_kind not null default 'chaleur',
    created_at timestamptz not null default now(),
    check (
        (camp_type = 'tselatra' and days <= 3)
        or (camp_type = 'lehibe' and days <= 5)
        or (camp_type = 'feno' and days <= 11)
    ),
    check (current_day between 1 and days)
);
create index campaigns_owner_idx on public.campaigns (owner_id);

create table public.activities (
    id uuid primary key default gen_random_uuid(),
    campaign_id uuid not null references public.campaigns (id) on delete cascade,
    title text not null check (char_length(title) between 1 and 160),
    day smallint not null check (day between 1 and 11),
    slot_start time not null,
    slot_end time not null,
    points integer not null check (points between 1 and 1000),
    cipher public.cipher_kind not null,
    encoded_text text not null,
    status public.activity_status not null default 'draft',
    created_at timestamptz not null default now(),
    check (slot_start < slot_end)
);
create index activities_campaign_day_idx on public.activities (campaign_id, day);

create table public.activity_secrets (
    activity_id uuid primary key references public.activities (id) on delete cascade,
    cipher_shift smallint check (cipher_shift between 1 and 25),
    cipher_key text,
    answer_digest char(64) not null,
    solution_sealed bytea not null
);

create table public.team_activity (
    activity_id uuid not null references public.activities (id) on delete cascade,
    team_id uuid not null references public.teams (id) on delete cascade,
    status public.progress_status not null default 'open',
    attempts_used smallint not null default 0 check (attempts_used >= 0),
    running boolean not null default false,
    deadline_at timestamptz,
    awarded_points integer not null default 0,
    updated_at timestamptz not null default now(),
    primary key (activity_id, team_id)
);
create index team_activity_team_idx on public.team_activity (team_id);

create table public.attempts (
    id bigint generated always as identity primary key,
    activity_id uuid not null references public.activities (id) on delete cascade,
    team_id uuid not null references public.teams (id) on delete cascade,
    number smallint not null,
    outcome public.attempt_outcome not null,
    recorded_at timestamptz not null default now()
);
create index attempts_team_activity_idx on public.attempts (team_id, activity_id);

create table public.team_camp_state (
    team_id uuid primary key references public.teams (id) on delete cascade,
    wood integer not null default 0 check (wood >= 0),
    water integer not null default 0 check (water >= 0),
    food integer not null default 0 check (food >= 0),
    missed_meal_days integer not null default 0 check (missed_meal_days >= 0),
    blocked_since timestamptz,
    sick boolean not null default false
);

create table public.team_day_actions (
    team_id uuid not null references public.teams (id) on delete cascade,
    campaign_id uuid not null references public.campaigns (id) on delete cascade,
    day smallint not null check (day >= 1),
    resource public.camp_resource not null,
    primary key (team_id, day, resource)
);
create index team_day_actions_campaign_idx on public.team_day_actions (campaign_id);
