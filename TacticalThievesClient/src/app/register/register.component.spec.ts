import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
// HttpClient providers for the "test" version: the component injects HttpClient, so DI
// must be able to resolve it (no real network call once AuthService is mocked).
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { RegisterComponent } from './register.component';
import { AuthService } from '../../services/auth-service/auth.service';

describe('RegisterComponent', () => {
  let component: RegisterComponent;
  let fixture: ComponentFixture<RegisterComponent>;
  // Fake AuthService: only the two methods used by the component.
  let authServiceMock: {
    register: jasmine.Spy;
    getErrorDetailForUser: jasmine.Spy;
  };
  // Fake Router: we watch navigate.
  let routerMock: { navigate: jasmine.Spy };

  // DOM helpers (same as in login.component.spec.ts).
  function getInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('input');
  }
  function getButton(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('button');
  }
  // Simulates user input in the field (updates ngModel).
  function typeUsername(value: string): void {
    const input = getInput();
    input.value = value;
    input.dispatchEvent(new Event('input')); // ngModel reacts to the 'input' event
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
      imports: [RegisterComponent], // standalone → also brings FormsModule (for ngModel)
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
  // Group A — Component & username binding
  // ===================================================================

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // A2: typing in the field updates the username property (two-way binding).
  it('should bind the input value to the username property', () => {
    typeUsername('Alice');
    expect(component.username).toBe('Alice');
  });

  // ===================================================================
  // Group B — Button click (length validation: < 3 characters)
  // ===================================================================

  // B1: empty username → prompt message, and we do NOT attempt to register.
  it('should reject an empty username', () => {
    getButton().click();
    fixture.detectChanges();

    expect(component.message).toBe('Username must be at least 3 characters long');
    expect(authServiceMock.register).not.toHaveBeenCalled();
  });

  // B2: username too short ('ab' → 2 characters) → same rejection (edge case < 3).
  it('should reject a username shorter than 3 characters', () => {
    typeUsername('ab');
    getButton().click();
    fixture.detectChanges();

    expect(component.message).toBe('Username must be at least 3 characters long');
    expect(authServiceMock.register).not.toHaveBeenCalled();
  });

  // B3: valid username → authService.register is called with that username.
  it('should call authService.register with a valid username', async () => {
    authServiceMock.register.and.resolveTo(true); // register returns a Promise<boolean>
    typeUsername('Alice');

    getButton().click();
    // onRegister() is asynchronous: we wait for pending promises to settle.
    await fixture.whenStable();

    expect(authServiceMock.register).toHaveBeenCalledWith('Alice');
  });

  // ===================================================================
  // Group C — `message` values depending on the register outcome
  // (we drive the async by calling onRegister() directly)
  // ===================================================================

  // C1: register succeeds → success message + redirect to /login.
  it('should navigate to /login and set success message on success', async () => {
    component.username = 'Alice';
    authServiceMock.register.and.resolveTo(true);

    await component.onRegister();

    expect(component.message).toBe('Registration successful !');
    expect(routerMock.navigate).toHaveBeenCalledWith(['/login']);
  });

  // C2: register fails (false) → "Registration failed" message, no navigation.
  it('should set message to "Registration failed" when register returns false', async () => {
    component.username = 'Alice';
    authServiceMock.register.and.resolveTo(false);

    await component.onRegister();

    expect(component.message).toBe('Registration failed');
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  // C3: register throws → "Registration failed" message + formatted detail.
  it('should set a "Registration failed" message when register throws', async () => {
    component.username = 'Alice';
    authServiceMock.register.and.rejectWith(new Error('network down'));

    await component.onRegister();

    expect(component.message).toContain('Registration failed');
    expect(component.message).toContain('boom'); // what getErrorDetailForUser (mock) returns
    expect(authServiceMock.getErrorDetailForUser).toHaveBeenCalled();
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });
});
