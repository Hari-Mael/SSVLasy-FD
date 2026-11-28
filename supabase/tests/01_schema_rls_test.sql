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

\set S   '00000000-0000-0000-0000-000000000001'
\set C1  '00000000-0000-0000-0000-000000000002'
\set C2  '00000000-0000-0000-0000-000000000003'
\set M1  '00000000-0000-0000-0000-000000000011'
\set M2  '00000000-0000-0000-0000-000000000012'
\set X1  '00000000-0000-0000-0000-000000000013'
\set X2  '00000000-0000-0000-0000-000000000014'
\set X3  '00000000-0000-0000-0000-000000000015'
\set X4  '00000000-0000-0000-0000-000000000016'
\set G   '00000000-0000-0000-0000-000000000017'
\set T1  '00000000-0000-0000-0000-000000000101'
\set T2  '00000000-0000-0000-0000-000000000102'
\set T3  '00000000-0000-0000-0000-000000000103'
\set T4  '00000000-0000-0000-0000-000000000104'
\set CAMP1 '00000000-0000-0000-0000-000000000201'
\set CAMP2 '00000000-0000-0000-0000-000000000202'
\set ACT_PUB '00000000-0000-0000-0000-000000000301'
\set ACT_DRAFT2 '00000000-0000-0000-0000-000000000302'

select pg_temp.as_admin();

insert into auth.users (id) select unnest(array[:'S', :'C1', :'C2', :'M1', :'M2', :'X1', :'X2', :'X3', :'X4', :'G']::uuid[]);
insert into auth.users (id) select ('e0000000-0000-0000-0000-' || lpad(n::text, 12, '0'))::uuid from generate_series(1, 10) n;

insert into public.profiles (id, role, display_name)
values (:'S', 'super_admin', 'Admin principal');
insert into public.profiles (id, role, display_name)
select ('e0000000-0000-0000-0000-' || lpad(n::text, 12, '0'))::uuid, 'super_admin', 'Admin ' || n
from generate_series(1, 9) n;
select pg_temp.expect_error(
    $q$insert into public.profiles (id, role, display_name)
       values ('e0000000-0000-0000-0000-000000000010', 'super_admin', 'Admin 11')$q$,
    'QUOTA_USERS_SUPER_ADMIN');

insert into public.profiles (id, role, display_name) values (:'C1', 'chef', 'Chef Un'), (:'C2', 'chef', 'Chef Deux');
select pg_temp.expect_error(
    format($q$insert into public.profiles (id, role, display_name) values (%L, 'member', 'Sans login')$q$, :'X4'),
    'profiles_check');

insert into public.profiles (id, role, login, display_name) values
    (:'M1', 'member', 'renard-rouge', 'Renard Rouge'),
    (:'M2', 'member', 'lion', 'Lion'),
    (:'X1', 'member', 'x1', 'X1'),
    (:'X2', 'member', 'x2', 'X2'),
    (:'X3', 'member', 'x3', 'X3'),
    (:'X4', 'member', 'x4', 'X4'),
    (:'G', 'member', 'gardien-manquant', 'Sans responsable');

insert into public.teams (id, chef_id, name) values
    (:'T1', :'C1', 'Équipe A'), (:'T2', :'C1', 'Équipe B'), (:'T3', :'C1', 'Équipe C');
select pg_temp.expect_error(
    format($q$insert into public.teams (id, chef_id, name) values (%L, %L, 'Équipe D')$q$, :'T4', :'C1'),
    'QUOTA_TEAMS_PER_CHEF');
select pg_temp.expect_error(
    format($q$insert into public.teams (chef_id, name) values (%L, 'Pas un chef')$q$, :'M2'),
    'OWNER_NOT_CHEF');

begin;
insert into public.team_members (user_id, team_id) values (:'M1', :'T1'), (:'M2', :'T1'), (:'X1', :'T1'), (:'X2', :'T1'), (:'X3', :'T1');
insert into public.member_guardians (user_id, parent_name, parent_contact) values
    (:'M1', 'Parent Un', '+261 34 00 000 01'), (:'M2', 'Parent Deux', '+261 34 00 000 02'),
    (:'X1', 'P X1', '1'), (:'X2', 'P X2', '2'), (:'X3', 'P X3', '3');
commit;

set constraints all immediate;
select pg_temp.expect_error(
    format($q$insert into public.team_members (user_id, team_id) values (%L, %L)$q$, :'G', :'T2'),
    'GUARDIAN_REQUIRED');
set constraints all deferred;

select pg_temp.expect_error(
    format($q$insert into public.team_members (user_id, team_id) values (%L, %L)$q$, :'X4', :'T1'),
    'QUOTA_MEMBERS_PER_TEAM');
select pg_temp.expect_error(
    format($q$insert into public.team_members (user_id, team_id) values (%L, %L)$q$, :'C1', :'T2'),
    'NOT_ACTIVE_MEMBER');

insert into public.campaigns (id, owner_id, name, camp_type, days, start_date) values
    (:'CAMP1', :'C1', 'Camp Lehibe', 'lehibe', 5, '2026-12-01'),
    (:'CAMP2', :'C2', 'Camp Tselatra', 'tselatra', 3, '2026-12-01');
select pg_temp.expect_error(
    format($q$insert into public.campaigns (owner_id, name, camp_type, days, start_date)
             values (%L, 'Trop long', 'tselatra', 4, '2026-12-01')$q$, :'C1'),
    'campaigns_check');
select pg_temp.expect_error(
    format($q$insert into public.campaigns (owner_id, name, camp_type, days, start_date)
             values (%L, 'Pas chef', 'feno', 2, '2026-12-01')$q$, :'M1'),
    'OWNER_NOT_CHEF');

insert into public.activities (id, campaign_id, title, day, slot_start, slot_end, points, cipher, encoded_text, status)
values (:'ACT_PUB', :'CAMP1', 'Énigme publiée', 1, '09:00', '10:00', 10, 'caesar', 'KHOOR', 'published');
insert into public.activities (campaign_id, title, day, slot_start, slot_end, points, cipher, encoded_text)
select :'CAMP1', 'Brouillon ' || n, 1, '11:00', '12:00', 5, 'atbash', 'ZYX' from generate_series(1, 6) n;
select pg_temp.expect_error(
    format($q$insert into public.activities (campaign_id, title, day, slot_start, slot_end, points, cipher, encoded_text)
             values (%L, 'Huitième', 1, '13:00', '14:00', 5, 'atbash', 'X')$q$, :'CAMP1'),
    'QUOTA_ACTIVITIES_PER_DAY');
insert into public.activities (id, campaign_id, title, day, slot_start, slot_end, points, cipher, encoded_text)
values (:'ACT_DRAFT2', :'CAMP1', 'Brouillon jour 2', 2, '09:00', '10:00', 5, 'morse', '... ---');
select pg_temp.expect_error(
    format($q$insert into public.activities (campaign_id, title, day, slot_start, slot_end, points, cipher, encoded_text)
             values (%L, 'Jour 6', 6, '09:00', '10:00', 5, 'morse', 'x')$q$, :'CAMP1'),
    'DAY_OUT_OF_CAMPAIGN');

insert into public.activity_secrets (activity_id, cipher_shift, answer_digest, solution_sealed)
values (:'ACT_PUB', 3, repeat('a', 64), '\x00'), (:'ACT_DRAFT2', null, repeat('b', 64), '\x01');

insert into public.team_activity (activity_id, team_id, status) values (:'ACT_PUB', :'T1', 'open');
insert into public.attempts (activity_id, team_id, number, outcome) values (:'ACT_PUB', :'T1', 1, 'wrong');
insert into public.team_camp_state (team_id) values (:'T1');
insert into public.team_day_actions (team_id, campaign_id, day, resource) values (:'T1', :'CAMP1', 1, 'wood');

select pg_temp.as_user(:'M1');
select pg_temp.check_count('M1 voit son équipe seulement', (select count(*) from public.teams), 1);
select pg_temp.check_count('M1 voit campagne de son chef seulement', (select count(*) from public.campaigns), 1);
select pg_temp.check_count('M1 ne voit que les activités publiées', (select count(*) from public.activities), 1);
select pg_temp.check_count('M1 ne voit aucun secret', (select count(*) from public.activity_secrets), 0);
select pg_temp.check_count('M1 ne voit aucun responsable légal', (select count(*) from public.member_guardians), 0);
select pg_temp.check_count('M1 ne voit que son profil', (select count(*) from public.profiles), 1);
select pg_temp.check_count('M1 voit son équipe (5)', (select count(*) from public.team_members), 5);
select pg_temp.check_count('M1 voit son état de jeu', (select count(*) from public.team_activity), 1);
select pg_temp.expect_error(
    format($q$insert into public.teams (chef_id, name) values (%L, 'Piratage')$q$, :'C2'),
    'permission denied');
select pg_temp.expect_error(
    format($q$insert into public.team_activity (activity_id, team_id) values (%L, %L)$q$, :'ACT_PUB', :'T1'),
    'permission denied');
select pg_temp.expect_error(
    format($q$update public.team_activity set awarded_points = 999$q$),
    'permission denied');

select pg_temp.as_user(:'C1');
select pg_temp.check_count('C1 voit ses 3 équipes', (select count(*) from public.teams), 3);
select pg_temp.check_count('C1 voit ses campagnes', (select count(*) from public.campaigns), 1);
select pg_temp.check_count('C1 voit toutes ses activités', (select count(*) from public.activities), 8);
select pg_temp.check_count('C1 voit les secrets de son camp', (select count(*) from public.activity_secrets), 2);
select pg_temp.check_count('C1 voit ses membres (soi + 5)', (select count(*) from public.profiles), 6);
select pg_temp.check_count('C1 voit les responsables de ses membres', (select count(*) from public.member_guardians), 5);
select pg_temp.expect_error(
    format($q$insert into public.campaigns (owner_id, name, camp_type, days, start_date)
             values (%L, 'Vol', 'feno', 2, '2026-12-01')$q$, :'C2'),
    'permission denied');
select pg_temp.expect_error(
    format($q$insert into public.teams (chef_id, name) values (%L, 'Vol')$q$, :'C2'),
    'permission denied');

select pg_temp.as_user(:'C2');
select pg_temp.check_count('C2 ne voit que son camp', (select count(*) from public.campaigns), 1);
select pg_temp.check_count('C2 ne voit aucune activité de C1', (select count(*) from public.activities), 0);
select pg_temp.check_count('C2 ne voit aucune équipe de C1', (select count(*) from public.teams), 0);

select pg_temp.as_user(null);
select pg_temp.expect_error($q$select 1 from public.teams$q$, 'permission denied');

select pg_temp.as_user(:'S');
select pg_temp.check_count('S voit tous les profils', (select count(*) from public.profiles), 19);
select pg_temp.check_count('S voit toutes les campagnes', (select count(*) from public.campaigns), 2);
select pg_temp.check_count('S voit les secrets', (select count(*) from public.activity_secrets), 2);

select pg_temp.as_user(:'M1');
select pg_temp.expect_error(format($q$update public.activities set status = 'unlocked' where id = %L$q$, :'ACT_DRAFT2'), 'permission denied');
select pg_temp.as_admin();
select pg_temp.check_count('brouillon non débloqué par un membre', (select count(*) from public.activities where id = :'ACT_DRAFT2' and status = 'draft'), 1);

update public.profiles set active = false where role = 'super_admin' and id <> :'S';
select pg_temp.expect_error(
    format($q$update public.profiles set active = false where id = %L$q$, :'S'),
    'LAST_SUPER_ADMIN');

select 'TOUS LES TESTS PASSENT' as resultat;
