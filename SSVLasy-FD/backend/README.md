# Backend Sahavola

Le jeu (`scripts/`, Godot 4.3 C#) est la référence des règles. Ce backend les reprend :
- essais : délai d'attente après chaque erreur (15, 10, 5 minutes), puis « Refaire » (une tentative sans délai) ou « Passer » ;
- codes : les 17 codes du jeu, avec ses tables ; les autres familles du cahier sont au catalogue mais refusées tant que leurs règles ne sont pas fournies ;
- durée des camps : 1 à 11 jours selon le type de camp ; 10 Super Admins maximum, 100 chefs, 3 équipes par chef, 5 membres par équipe, 7 activités par jour ;
- limitation des tentatives de connexion : 5 échecs par identifiant sur 15 minutes.

- comptes membres : connexion par pseudonyme et mot de passe (le code d'accès est supprimé) ; le pseudonyme sert d'identifiant unique.

- vie du camp (module `camp_life`) : collecte bois/eau/nourriture une fois par jour et par équipe, repas automatiques au passage au jour suivant, réserve de 15 bois pour la nuit, blocage après 2 jours sans repas, notification à 15 minutes, maladie à 30 minutes, déblocage par le chef, énergie des membres, météo, type de camp (1 à 11 jours selon le type). Une équipe bloquée ou malade ne peut plus jouer.

Différences avec le jeu à trancher : le backend ne fait pas décroître l'énergie dans le temps (seulement aux repas) ; le passage au jour suivant reste manuel, par le chef ou le Super Admin.

Restent à porter : le planificateur automatique des jours (au lieu du passage manuel), le rapport quotidien de 19h à 20h, et les textes en trois langues.

---

# Jeu scout de camp — backend

Phase 1 du planning : socle (accès, équipes, campagnes) et moteur d'énigmes codées.
Architecture : monolithe modulaire (plan B) avec règles pures dans `domain/`, cas d'usage dans `application/`, SQL dans `infrastructure/`, HTTP dans `api/`.

## Démarrage

```
docker compose up -d
cd backend
python -m venv .venv && source .venv/bin/activate
pip install -e ".[dev]"
cp .env.example .env
python -m app.migrate
psql "$SCOUT_MIGRATION_DATABASE_URL" -c "ALTER ROLE scout_app PASSWORD 'scout_app'"
python -m app.create_super_admin admin@exemple.mg "Nom Prénom"
uvicorn app.main:app --reload
pytest --cov=app
ruff check . && mypy app
```

Le rôle `scout_app` n'est ni propriétaire ni superutilisateur : c'est ce qui rend les politiques row-level security effectives. Ne jamais connecter l'API avec un superutilisateur.

## Variables d'environnement

| Variable | Rôle |
| --- | --- |
| `SCOUT_DATABASE_URL` | connexion de l'API (rôle `scout_app`) |
| `SCOUT_MIGRATION_DATABASE_URL` | connexion des migrations (propriétaire, optionnelle) |
| `SCOUT_JWT_SECRET` | signature des jetons |
| `SCOUT_JWT_TTL_MINUTES` | durée de vie des jetons (défaut 720) |
| `SCOUT_ANSWER_HMAC_SECRET` | HMAC des réponses attendues |
| `SCOUT_CONTENT_ENCRYPTION_KEY` | clé Fernet des messages en clair |
| `SCOUT_DEFAULT_TIMEZONE` | fuseau par défaut des campagnes |

Générer un secret : `python -c "import secrets; print(secrets.token_urlsafe(48))"`.
Générer la clé Fernet : `python -c "from cryptography.fernet import Fernet; print(Fernet.generate_key().decode())"`.

## API `/api/v1`

| Méthode et chemin | Qui | Rôle |
| --- | --- | --- |
| `POST /auth/staff-login` | public | connexion chef ou super admin |
| `POST /auth/member-login` | public | connexion membre par code d'accès |
| `POST /staff-users` | super admin | crée un super admin ou un chef |
| `POST /teams`, `GET /teams` | chef (création), chef et super admin (liste) | équipes |
| `POST /teams/{id}/members`, `GET` | chef, super admin | membres, le code d'accès n'est affiché qu'à la création |
| `POST /campaigns`, `GET /campaigns` | chef (création), tous (liste) | campagnes |
| `POST /campaigns/{id}/transitions` | chef, super admin | brouillon, planifiée, en cours, en pause, terminée |
| `POST /campaigns/{id}/activities`, `GET` | chef, super admin (création), tous (liste) | énigmes codées |
| `POST /activities/{id}/publish`, `/unlock` | chef, super admin | publication et déblocage |
| `GET /activities/{id}/solution` | chef, super admin | message en clair |
| `POST /activities/{id}/attempts`, `/answers`, `/choices`, `GET /progress` | membre | jeu d'une équipe |

## Règles appliquées

- Quotas : 10 super admins, 100 chefs, 3 équipes par chef, 5 membres par équipe, 7 activités par jour, 1 à 11 jours selon le type de camp (Tselatra 1–3, Lehibe 1–5, Feno 1–11). Contrôlés dans l'API et par triggers PostgreSQL (verrous consultatifs contre les créations simultanées). Les valeurs sont dupliquées dans `migrations/0001_init.sql` (impossible à partager avec Python).
- Le dernier super admin actif ne peut pas être retiré.
- Planification : au moins 6 chefs actifs et une date de début non passée.
- Isolation : chaque chef ne voit que ses données (RLS) ; un membre ne voit que son équipe et les activités publiées.
- Réponses : comparaison par HMAC sans accents, casse, espaces ni ponctuation ; message en clair chiffré au repos.
- Essais : 3 essais de 15, 10 et 5 minutes. Après le 3e échec : « refaire » (sans délai, un seul essai) ou « passer ». Après l'échec du « refaire » : « passer » seulement.
- Activité de nuit (début à 21 h ou plus) : fermée aux équipes qui n'ont pas résolu toutes les activités du jour finissant avant 17 h 30.
- Membres : pas de compte e-mail ; un code d'accès aléatoire par membre, stocké haché.

## Interprétations à confirmer

- Les délais 15 / 10 / 5 minutes sont lus comme le temps alloué à chaque essai.
- La progression est suivie par équipe, pas par membre.
- Une activité « passée » compte comme non accomplie pour l'accès aux activités de nuit. La reprise après la pause de midi n'est pas encore implémentée.

## Prochaines étapes

- Journal d'événements et synchronisation hors-ligne (plan C), scheduler persistant pour déblocage et passage des états.
- PostGIS : géofences, positions, SOS, purge en fin de session.
- Limitation du nombre de tentatives de connexion, journal d'audit, rapports du soir, scores et badges.
- Autres modèles d'activité (chasse au trésor, jeu de nuit, veillée) et fiches des codes malgaches.
- Tests d'intégration sur une vraie base PostgreSQL.
