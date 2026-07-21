import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
// Fournisseurs HttpClient pour la version "test" : le composant injecte HttpClient,
// il faut donc que l'injection de dépendances puisse le résoudre (aucun vrai appel réseau).
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth-service/auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  // Faux AuthService : on ne garde que les deux méthodes utilisées par le composant.
  let authServiceMock: {
    login: jasmine.Spy;
    getErrorDetailForUser: jasmine.Spy;
  };
  // Faux Router : on surveille juste navigate.
  let routerMock: { navigate: jasmine.Spy };

  // Utilitaires DOM pour éviter la répétition.
  function getInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input');
  }
  function getButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button');
  }
  // Simule une saisie utilisateur dans le champ (met à jour ngModel).
  function typeUsername(value: string): void {
    const input = getInput();
    input.value = value;
    input.dispatchEvent(new Event('input')); // ngModel réagit à l'événement 'input'
    fixture.detectChanges();
  }

  beforeEach(async () => {
    authServiceMock = {
      login: jasmine.createSpy('login'),
      // Par défaut, le formatage d'erreur renvoie 'boom' (valeur de test arbitraire).
      getErrorDetailForUser: jasmine
        .createSpy('getErrorDetailForUser')
        .and.returnValue('boom'),
    };
    routerMock = { navigate: jasmine.createSpy('navigate') };

    await TestBed.configureTestingModule({
      imports: [LoginComponent], // standalone → apporte aussi FormsModule (pour ngModel)
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ===================================================================
  // Groupe A — Composant & liaison du username
  // ===================================================================

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // A2 : saisir dans le champ met bien à jour la propriété username (two-way binding).
  it('should bind the input value to the username property', () => {
    typeUsername('Alice');
    expect(component.username).toBe('Alice');
  });

  // ===================================================================
  // Groupe B — Clic sur le bouton (avec / sans username)
  // ===================================================================

  // B1 : clic sans username → message d'invite, et on ne tente PAS de se connecter.
  it('should show "Please enter a username" when clicking with empty username', () => {
    // username est vide (état initial), on clique directement.
    getButton().click();
    fixture.detectChanges();

    expect(component.message).toBe('Please enter a username');
    // On n'a pas appelé le service de connexion puisque le username manque.
    expect(authServiceMock.login).not.toHaveBeenCalled();
    // Le message est aussi réellement affiché à l'écran.
    expect(fixture.nativeElement.querySelector('p').textContent).toContain(
      'Please enter a username',
    );
  });

  // B2 : clic avec un username → authService.login est appelé avec ce username.
  it('should call authService.login with the username when clicking with a username', async () => {
    authServiceMock.login.and.resolveTo(true); // login renvoie une Promise<boolean>
    typeUsername('Alice');

    getButton().click();
    // onLogin() est asynchrone : on attend que les promesses en attente se règlent.
    await fixture.whenStable();

    expect(authServiceMock.login).toHaveBeenCalledWith('Alice');
  });

  // ===================================================================
  // Groupe C — Valeurs de `message` selon l'issue de login
  // (on pilote l'async en appelant directement onLogin())
  // ===================================================================

  // C1 : login réussit → redirection vers /home, message resté à 'Starting login...'.
  it('should navigate to /home on successful login', async () => {
    component.username = 'Alice';
    authServiceMock.login.and.resolveTo(true);

    await component.onLogin();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/home']);
    expect(component.message).toBe('Starting login...');
  });

  // C2 : login échoue (false) → message d'erreur "token manquant", pas de navigation.
  it('should set message to "Error: Missing token" when login returns false', async () => {
    component.username = 'Alice';
    authServiceMock.login.and.resolveTo(false);

    await component.onLogin();

    expect(component.message).toBe('Error: Missing token');
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  // C3 : login lève une exception → message "Login failed" + détail formaté, pas de navigation.
  it('should set a "Login failed" message when login throws', async () => {
    component.username = 'Alice';
    authServiceMock.login.and.rejectWith(new Error('network down'));

    await component.onLogin();

    expect(component.message).toContain('Login failed');
    expect(component.message).toContain('boom'); // ce que renvoie getErrorDetailForUser (mock)
    expect(authServiceMock.getErrorDetailForUser).toHaveBeenCalled();
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

});
