# Backend Supabase — Sahavola

Le backend est entièrement dans Supabase : Postgres (schéma et règles dans `migrations/`), Auth, Edge Functions (`functions/`) et tests (`tests/`). Le dossier `backend/` (FastAPI) est déprécié et sera retiré.

## Structure

- `migrations/` : schéma consolidé, appliqué dans l'ordre des horodatages.
- `functions/create-member/` : création d'un membre (compte + équipe + responsable légal, en une seule opération).
- `tests/` : vérifications SQL (quotas, RLS par rôle, équipes, campagnes et activités). À exécuter sur une base locale migrée.

## Rôles

- `super_admin` : gère le camp global (10 maximum).
- `chef` : crée ses équipes (3 maximum) et ses membres (5 par équipe). Chefs créés par un super admin (100 maximum).
- `member` : se connecte avec son pseudonyme. Ne voit que son équipe et les activités publiées.

## Tests locaux

Prérequis : Docker, `supabase` CLI, `psql`.

```
supabase start
for f in supabase/tests/*.sql; do
    psql "postgresql://postgres:postgres@127.0.0.1:54322/postgres" -v ON_ERROR_STOP=1 -f "$f"
done
supabase stop
```

Les fichiers s'exécutent dans l'ordre de leur nom (préfixes 01, 02, 03) : chaque suite réutilise les données de la précédente.

## Mise en place sur le projet distant (une seule fois)

1. Authentication, paramètres : désactiver l'inscription publique (« Allow new users to sign up ») : seuls les RPC et Edge Functions créent des comptes. Longueur minimale du mot de passe : 8.
2. Dans le dépôt : `supabase init` (répondre non à l'intégration éditeur si proposée), puis `supabase login` et `supabase link --project-ref <ref>`.
3. `supabase db push` : applique les migrations. Si la base contient déjà l'ancien schéma (tests manuels dans l'éditeur SQL), réinitialisez-la avant (Project Settings, Database).
4. `supabase functions deploy create-member`.
5. Premier super admin : Authentication, Users, « Add user » (e-mail réel, mot de passe, compte confirmé). Puis, dans l'éditeur SQL, avec l'UUID du compte :

```
insert into public.profiles (id, role, display_name)
values ('<uuid>', 'super_admin', 'Nom affiché');
```

6. Chefs : même procédure, avec `role = 'chef'`, jusqu'à la création de la fonction `create-staff`.

## Secrets GitHub (Settings, Secrets and variables, Actions)

- `SUPABASE_ACCESS_TOKEN` : jeton personnel (Account, Access Tokens).
- `SUPABASE_PROJECT_ID` : référence du projet.
- `SUPABASE_DB_PASSWORD` : mot de passe de la base.

## Règles de sécurité

- Le client Godot n'utilise que la clé `anon`. La clé `service_role` reste dans les Edge Functions et les secrets CI.
- Toute écriture de l'état de jeu passe par une fonction serveur, jamais par une écriture directe.
- Plan Free : le projet est mis en pause après 7 jours d'inactivité. Passer au plan Pro avant la mise en production.
