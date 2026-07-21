import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
// Fournisseurs HttpClient version "test" : le composant injecte HttpClient, la DI doit
// pouvoir le résoudre (aucun vrai appel réseau une fois AuthService mocké).
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../services/auth-service/auth.service';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  // Faux AuthService : uniquement les deux méthodes utilisées par le composant.
  let authServiceMock: {
    register: jasmine.Spy;
    getErrorDetailForUser: jasmine.Spy;
  };
  // Faux Router : on surveille navigate.
  let routerMock: { navigate: jasmine.Spy };

  // Utilitaires DOM (mêmes que dans login.component.spec.ts).
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
      register: jasmine.createSpy('register'),
      getErrorDetailForUser: jasmine
        .createSpy('getErrorDetailForUser')
        .and.returnValue('boom'),
    };
    routerMock = { navigate: jasmine.createSpy('navigate') };

    await TestBed.configureTestingModule({
      imports: [RegisterComponent], // standalone → apporte aussi FormsModule (pour ngModel)
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceMock },
        { provide: Router, useValue: routerMock },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
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
  // Groupe B — Clic sur le bouton (validation de longueur : < 3 caractères)
  // ===================================================================

  // B1 : username vide → message d'invite, et on ne tente PAS de s'inscrire.
  it('should reject an empty username', () => {
    getButton().click();
    fixture.detectChanges();

    expect(component.message).toBe('Username must be at least 3 characters long');
    expect(authServiceMock.register).not.toHaveBeenCalled();
  });

  // B2 : username trop court ('ab' → 2 caractères) → même rejet (cas limite < 3).
  it('should reject a username shorter than 3 characters', () => {
    typeUsername('ab');
    getButton().click();
    fixture.detectChanges();

    expect(component.message).toBe('Username must be at least 3 characters long');
    expect(authServiceMock.register).not.toHaveBeenCalled();
  });

  // B3 : username valide → authService.register est appelé avec ce username.
  it('should call authService.register with a valid username', async () => {
    authServiceMock.register.and.resolveTo(true); // register renvoie une Promise<boolean>
    typeUsername('Alice');

    getButton().click();
    // onRegister() est asynchrone : on attend le règlement des promesses en attente.
    await fixture.whenStable();

    expect(authServiceMock.register).toHaveBeenCalledWith('Alice');
  });

  // ===================================================================
  // Groupe C — Valeurs de `message` selon l'issue de register
  // (on pilote l'async en appelant directement onRegister())
  // ===================================================================

  // C1 : register réussit → message de succès + redirection vers /login.
  it('should navigate to /login and set success message on success', async () => {
    component.username = 'Alice';
    authServiceMock.register.and.resolveTo(true);

    await component.onRegister();

    expect(component.message).toBe('Registration successful !');
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
  });

  // C2 : register échoue (false) → message "Registration failed", pas de navigation.
  it('should set message to "Registration failed" when register returns false', async () => {
    component.username = 'Alice';
    authServiceMock.register.and.resolveTo(false);

    await component.onRegister();

    expect(component.message).toBe('Registration failed');
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  // C3 : register lève une exception → message "Registration failed" + détail formaté.
  it('should set a "Registration failed" message when register throws', async () => {
    component.username = 'Alice';
    authServiceMock.register.and.rejectWith(new Error('network down'));

    await component.onRegister();

    expect(component.message).toContain('Registration failed');
    expect(component.message).toContain('boom'); // ce que renvoie getErrorDetailForUser (mock)
    expect(authServiceMock.getErrorDetailForUser).toHaveBeenCalled();
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });
});
