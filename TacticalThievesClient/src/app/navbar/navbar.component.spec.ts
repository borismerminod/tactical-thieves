// Outils Angular de test : TestBed (banc de test) et ComponentFixture (accès instance + DOM).
import { ComponentFixture, TestBed } from '@angular/core/testing';
// Router : la dépendance de navigation. provideRouter fournit un vrai router de test
// (indispensable pour que les routerLink du template se rendent correctement).
import { Router, provideRouter } from '@angular/router';
// BehaviorSubject : un Observable qui garde sa dernière valeur. On s'en sert pour SIMULER
// l'état de connexion et le pousser (.next(...)) depuis les tests.
import { BehaviorSubject } from 'rxjs';
// Le composant testé.
import { NavbarComponent } from './navbar.component';
// Le service qu'on va remplacer par un faux (mock).
import { AuthService } from '../../services/auth-service/auth.service';
// Les routes réelles de l'app, pour que provideRouter connaisse /home, /login, etc.
import { routes } from '../app.routes';

describe('NavbarComponent', () => {
  let component: NavbarComponent;
  let fixture: ComponentFixture<NavbarComponent>;
  // Notre faux AuthService : deux BehaviorSubject pilotables + un spy sur logout.
  let authServiceMock: {
    isLoggedIn$: BehaviorSubject<boolean>;
    username$: BehaviorSubject<string>;
    logout: jasmine.Spy;
  };
  let router: Router;

  // Petit utilitaire : renvoie tous les liens <a> actuellement rendus dans le DOM.
  function getLinks(): HTMLAnchorElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('a'));
  }

  // Retrouve un lien précis d'après sa cible (son attribut href généré par routerLink).
  function getLinkByHref(href: string): HTMLAnchorElement | undefined {
    return getLinks().find((a) => a.getAttribute('href') === href);
  }

  beforeEach(async () => {
    // On construit le faux service. isLoggedIn$ démarre à false (utilisateur déconnecté).
    authServiceMock = {
      isLoggedIn$: new BehaviorSubject<boolean>(false),
      username$: new BehaviorSubject<string>(''),
      logout: jasmine.createSpy('logout'),
    };

    await TestBed.configureTestingModule({
      imports: [NavbarComponent], // composant standalone
      providers: [
        provideRouter(routes),                                // vrai router (pour les routerLink)
        { provide: AuthService, useValue: authServiceMock },  // on injecte notre mock
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NavbarComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router); // on récupère l'instance du router pour l'espionner
    // On espionne navigate pour vérifier la redirection SANS réellement changer de page.
    spyOn(router, 'navigate');
    fixture.detectChanges(); // premier rendu
  });

  // ===================================================================
  // Groupe A — Liens de routage
  // ===================================================================

  // A1 : le composant s'instancie sans erreur.
  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // A2 : les liens Home et Game sont toujours présents et pointent vers les bonnes routes.
  it('should always display Home and Game links with correct routes', () => {
    const links = getLinks();
    // routerLink génère un attribut href : on retrouve nos deux cibles permanentes.
    const hrefs = links.map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/home');
    expect(hrefs).toContain('/unity-game');
  });

  // A3 : ACTION — cliquer sur Home déclenche une navigation vers /home.
  it('should navigate to /home when the Home link is clicked', () => {
    // Un clic sur un routerLink appelle navigateByUrl (PAS navigate) : c'est donc
    // cette méthode qu'on espionne ici. On l'intercepte pour éviter une vraie navigation.
    const navigateByUrlSpy = spyOn(router, 'navigateByUrl');

    // On simule le clic de l'utilisateur sur le lien Home.
    getLinkByHref('/home')!.click();

    // Une navigation a bien été déclenchée...
    expect(navigateByUrlSpy).toHaveBeenCalled();
    // ...et vers la bonne URL (l'argument reçu, converti en texte, vaut "/home").
    expect(navigateByUrlSpy.calls.mostRecent().args[0].toString()).toBe('/home');
  });

  // A4 : ACTION — cliquer sur Game déclenche une navigation vers /unity-game.
  it('should navigate to /unity-game when the Game link is clicked', () => {
    const navigateByUrlSpy = spyOn(router, 'navigateByUrl');

    getLinkByHref('/unity-game')!.click();

    expect(navigateByUrlSpy).toHaveBeenCalled();
    expect(navigateByUrlSpy.calls.mostRecent().args[0].toString()).toBe('/unity-game');
  });

  // A5 : ACTION — cliquer sur Register déclenche une navigation vers /register.
  // (Le lien Register n'existe que si l'utilisateur est déconnecté, ce qui est l'état
  //  initial de notre mock : rien de spécial à faire ici.)
  it('should navigate to /register when the Register link is clicked', () => {
    const navigateByUrlSpy = spyOn(router, 'navigateByUrl');

    getLinkByHref('/register')!.click();

    expect(navigateByUrlSpy).toHaveBeenCalled();
    expect(navigateByUrlSpy.calls.mostRecent().args[0].toString()).toBe('/register');
  });

  // ===================================================================
  // Groupe B — Utilisateur connecté vs non connecté
  // ===================================================================

  // B1 : déconnecté (état initial) → on voit Register et Login, pas de Logout.
  it('should show Register and Login when logged out', () => {
    const hrefs = getLinks().map((a) => a.getAttribute('href'));
    expect(hrefs).toContain('/register');
    expect(hrefs).toContain('/login');

    // Aucun lien ne doit contenir le texte "Logout".
    const hasLogout = getLinks().some((a) => a.textContent?.includes('Logout'));
    expect(hasLogout).toBeFalse();
  });

  // B2 : connecté → on voit "Logout <username>", et plus les liens Register/Login.
  it('should show Logout with username when logged in', () => {
    // On simule la connexion en poussant de nouvelles valeurs dans le mock.
    authServiceMock.isLoggedIn$.next(true);
    authServiceMock.username$.next('Alice');
    fixture.detectChanges(); // on redéclenche le rendu pour prendre en compte le nouvel état

    // Le lien de logout affiche le pseudo.
    const logoutLink = getLinks().find((a) => a.textContent?.includes('Logout'));
    expect(logoutLink).toBeTruthy();
    expect(logoutLink!.textContent).toContain('Alice');

    // Les liens de connexion/inscription ont disparu.
    const hrefs = getLinks().map((a) => a.getAttribute('href'));
    expect(hrefs).not.toContain('/register');
    expect(hrefs).not.toContain('/login');
  });

  // ===================================================================
  // Groupe C — Fonctionnalité logout
  // ===================================================================

  // C1 : cliquer sur le lien Logout appelle la méthode logout() du composant.
  it('should call logout() when the Logout link is clicked', () => {
    // On se met en état connecté pour que le lien Logout existe dans le DOM.
    authServiceMock.isLoggedIn$.next(true);
    fixture.detectChanges();

    // On espionne la méthode du composant.
    spyOn(component, 'logout');

    // On récupère le lien Logout et on simule le clic.
    const logoutLink = getLinks().find((a) => a.textContent?.includes('Logout'));
    logoutLink!.click();

    // Le binding (click)="logout()" a bien déclenché la méthode.
    expect(component.logout).toHaveBeenCalled();
  });

  // C2 : logout() déconnecte via le service PUIS redirige vers /home.
  it('should log out and redirect to /home', () => {
    component.logout();

    // Effet 1 : la déconnexion est demandée au service.
    expect(authServiceMock.logout).toHaveBeenCalled();
    // Effet 2 : la redirection vers l'accueil est déclenchée.
    expect(router.navigate).toHaveBeenCalledWith(['/home']);
  });
});
