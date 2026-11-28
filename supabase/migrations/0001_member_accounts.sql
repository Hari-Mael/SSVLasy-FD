-- Profils des utilisateurs Sahavola, liés à Supabase Auth.
-- Les membres se connectent par pseudonyme : leur e-mail technique est
-- <login>@members.sahavola.app (aucun e-mail n'est envoyé à cette adresse).

create table public.profiles (
    id uuid primary key references auth.users (id) on delete cascade,
    login text not null unique
        check (login ~ '^[a-z0-9-]{1,40}$'),
    display_name text not null
        check (char_length(display_name) between 1 and 40),
    role text not null
        check (role in ('super_admin', 'chef', 'membre')),
    chef_id uuid references public.profiles (id) on delete restrict,
    parent_name text,
    parent_contact text,
    active boolean not null default true,
    created_at timestamptz not null default now(),
    -- Un membre doit toujours avoir un chef ; un chef ou un super admin n'en a pas.
    check ((role = 'membre') = (chef_id is not null)),
    -- Un membre mineur doit avoir un responsable légal renseigné (consentement).
    check (role <> 'membre' or (parent_name is not null and parent_contact is not null))
);

-- Renvoie le rôle de l'utilisateur courant sans passer par la RLS (évite la récursion).
create or replace function public.current_role_name()
returns text
language sql
stable
security definer
set search_path = public
as $$
    select role from public.profiles where id = auth.uid()
$$;

alter table public.profiles enable row level security;

create policy profiles_read_self on public.profiles
    for select using (id = auth.uid());

create policy profiles_read_own_members on public.profiles
    for select using (chef_id = auth.uid());

create policy profiles_read_super_admin on public.profiles
    for select using (public.current_role_name() = 'super_admin');

-- Aucune écriture directe par les clients : tout passe par les fonctions serveur.
revoke insert, update, delete on public.profiles from anon, authenticated;

-- Quota : 10 super admins, 100 chefs, 15 membres par chef (3 équipes × 5).
create or replace function public.enforce_profile_quotas()
returns trigger
language plpgsql
as $$
begin
    if new.role = 'super_admin'
       and (select count(*) from public.profiles where role = 'super_admin') >= 10 then
        raise exception 'QUOTA_SUPER_ADMIN' using errcode = 'P0001';
    end if;
    if new.role = 'chef'
       and (select count(*) from public.profiles where role = 'chef') >= 100 then
        raise exception 'QUOTA_CHEF' using errcode = 'P0001';
    end if;
    if new.role = 'membre'
       and (select count(*) from public.profiles where chef_id = new.chef_id) >= 15 then
        raise exception 'QUOTA_MEMBRE_PAR_CHEF' using errcode = 'P0001';
    end if;
    return new;
end;
$$;

create trigger profiles_quotas
    before insert on public.profiles
    for each row execute function public.enforce_profile_quotas();
