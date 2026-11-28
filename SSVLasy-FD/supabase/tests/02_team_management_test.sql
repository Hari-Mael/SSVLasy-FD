\set ON_ERROR_STOP 1
create or replace function pg_temp.expect_error(stmt text, msg text) returns void language plpgsql as $$
begin
    execute stmt;
    raise exception 'ATTENDU ERREUR "%" mais succès : %', msg, stmt;
exception when others then
    if sqlerrm not like '%' || msg || '%' then
        raise exception 'MAUVAISE ERREUR (attendu "%") : % sur %', msg, sqlerrm, stmt;
    end if;
    raise notice 'OK erreur attendue : %', msg;
end $$;

create or replace function pg_temp.check_count(label text, actual bigint, expected bigint) returns void language plpgsql as $$
begin
    if actual <> expected then
        raise exception 'ECHEC % : attendu %, obtenu %', label, expected, actual;
    end if;
    raise notice 'OK % = %', label, actual;
end $$;

create or replace function pg_temp.as_user(uid uuid) returns void language plpgsql as $$
begin
    perform set_config('request.jwt.claim.sub', coalesce(uid::text, ''), false);
    execute case when uid is null then 'set role anon' else 'set role authenticated' end;
end $$;

create or replace function pg_temp.as_admin() returns void language plpgsql as $$
begin
    reset role;
    perform set_config('request.jwt.claim.sub', '', false);
end $$;


\set C1 '00000000-0000-0000-0000-000000000002'
\set C2 '00000000-0000-0000-0000-000000000003'
\set M1 '00000000-0000-0000-0000-000000000011'
\set X4 '00000000-0000-0000-0000-000000000016'
\set T1 '00000000-0000-0000-0000-000000000101'
\set T2 '00000000-0000-0000-0000-000000000102'
\set N1 '00000000-0000-0000-0000-000000000021'
\set N2 '00000000-0000-0000-0000-000000000022'
\set N3 '00000000-0000-0000-0000-000000000023'

create or replace function pg_temp.as_service() returns void language plpgsql as $f$
begin
    reset role;
    perform set_config('request.jwt.claim.sub', '', false);
    execute 'set role service_role';
end $f$;

insert into auth.users (id) values (:'N1'), (:'N2'), (:'N3');

select pg_temp.as_user(:'C2');
select public.create_team('Équipe Zéro');
select pg_temp.check_count('chef crée une équipe', (select count(*) from public.teams where chef_id = :'C2'), 1);
select pg_temp.expect_error($q$select public.create_team('Piratage')$q$, 'REAL_CHEF_REQUIRED');
select pg_temp.expect_error(format($q$insert into public.teams (chef_id, name) values (%L, 'Direct')$q$, :'C2'), 'permission denied');
select pg_temp.as_user(:'M1');
select pg_temp.expect_error($q$select public.create_team('Membre')$q$, 'REAL_CHEF_REQUIRED');
select pg_temp.as_user(null);
select pg_temp.expect_error($q$select public.create_team('Anonyme')$q$, 'permission denied');
select pg_temp.as_user(:'C1');
select pg_temp.expect_error(format($q$select public.register_member(%L, %L, %L, 'interdit', 'Interdit', 'P', 'C')$q$, :'C1', :'T1', :'N1'), 'permission denied');

select pg_temp.as_service();
select pg_temp.expect_error(format($q$select public.register_member(%L, %L, %L, 'mauvais-chef', 'Mauvais', 'P', 'C')$q$, :'C2', :'T2', :'N1'), 'TEAM_NOT_OWNED');
select public.register_member(:'C1', :'T2', :'N1', 'nouveau-membre', 'Nouveau', 'Parent', '+261 34 00 000 09');
select pg_temp.as_admin();
select pg_temp.check_count('membre enregistré dans l équipe', (select count(*) from public.team_members where user_id = :'N1' and team_id = :'T2'), 1);
select pg_temp.check_count('responsable enregistré', (select count(*) from public.member_guardians where user_id = :'N1'), 1);
select pg_temp.as_service();
select pg_temp.expect_error(format($q$select public.register_member(%L, %L, %L, 'nouveau-membre', 'Doublon', 'P', 'C')$q$, :'C1', :'T2', :'N2'), 'profiles_login_key');
select pg_temp.as_admin();
select pg_temp.check_count('doublon non créé', (select count(*) from public.profiles where id = :'N2'), 0);
select pg_temp.as_service();
select pg_temp.expect_error(format($q$select public.register_member(%L, %L, %L, 'complet', 'Complet', 'P', 'C')$q$, :'C1', :'T1', :'N3'), 'QUOTA_MEMBERS_PER_TEAM');
select pg_temp.as_admin();
select pg_temp.check_count('création annulée en bloc (équipe pleine)', (select count(*) from public.profiles where id = :'N3'), 0);

select pg_temp.as_admin();
select 'TESTS EQUIPES OK' as resultat;
