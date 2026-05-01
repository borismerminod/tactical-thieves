
# 🎮 Tactical-Thieves : Application Fullstack Temps Réel (Unity / ASP.NET / Angular)

## 📌 Description

Ce projet est un **proof of concept** d’application fullstack combinant un moteur de jeu Unity (WebGL) et une interface web Angular, communiquant via un backend ASP.NET Core.

L’objectif est de démontrer la mise en place d’une architecture moderne, intégrant :
- communication temps réel
- authentification sécurisée
- séparation front / backend / moteur

---

## 🧱 Architecture

Le projet est composé de trois couches principales :

- **Frontend (Angular)**  
  Interface utilisateur web

- **Backend (ASP.NET Core)**  
  API REST + gestion des communications temps réel

- **Client Unity (WebGL)**  
  Logique applicative et gameplay

Les échanges sont réalisés via :
- API REST (requêtes classiques)
- WebSockets (SignalR) pour le temps réel

---

## ⚙️ Technologies utilisées

### Backend
- ASP.NET Core
- SignalR (WebSockets)
- SQL Server

### Frontend
- Angular
- TypeScript

### Client
- Unity (WebGL)

### Autres
- Authentification par passkeys (WebAuthn)
- Docker (conteneurisation)
- Git

---

## 🔐 Authentification

Le système d’authentification repose sur les passkeys (WebAuthn) :
- pas de mot de passe
- authentification forte
- expérience utilisateur simplifiée

---

## 🔄 Fonctionnement

1. L’utilisateur accède à l’interface Angular
2. Il s’authentifie via passkey
3. Le frontend communique avec le backend via API REST
4. Le backend orchestre les échanges avec le client Unity
5. Les interactions en temps réel passent par SignalR

---

## 🚀 Objectifs du projet

- Expérimenter une architecture fullstack
- Mettre en place des communications temps réel
- Intégrer un moteur externe (Unity) dans une application web
- Explorer des solutions d’authentification modernes

---

## ⚠️ Limites

- Projet volontairement simplifié (proof of concept)
- Déployé localement (exposition via ngrok)
- Gameplay minimal (focus sur l’architecture)

---

## ▶️ Accès au projet

- 🔗 Application : https://mozell-fortifiable-moshe.ngrok-free.dev
- 💻 Code source : https://github.com/borismerminod/tactical-thieves

---

## 👨‍💻 Auteur

Projet réalisé dans une démarche de montée en compétences sur les architectures web modernes et les communications temps réel.
