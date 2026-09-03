import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
// HttpClient providers for the "test" version: the component injects HttpClient,
// so dependency injection must be able to resolve it (no real network call).
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth-service/auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  // Fake AuthService: we only keep the two methods used by the component.
  let authServiceMock: {
    login: jasmine.Spy;
    getErrorDetailForUser: jasmine.Spy;
  };
  // Fake Router: we only watch navigate.
  let routerMock: { navigate: jasmine.Spy };

  // DOM helpers to avoid repetition.
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
      login: jasmine.createSpy('login'),
      // By default, the error formatting returns 'boom' (arbitrary test value).
      getErrorDetailForUser: jasmine
        .createSpy('getErrorDetailForUser')
        .and.returnValue('boom'),
    };
    routerMock = { navigate: jasmine.createSpy('navigate') };

    await TestBed.configureTestingModule({
      imports: [LoginComponent], // standalone → also brings FormsModule (for ngModel)
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
  // Group B — Button click (with / without username)
  // ===================================================================

  // B1: click with no username → prompt message, and we do NOT attempt to log in.
  it('should show "Please enter a username" when clicking with empty username', () => {
    // username is empty (initial state), we click directly.
    getButton().click();
    fixture.detectChanges();

    expect(component.message).toBe('Please enter a username');
    // We did not call the login service since the username is missing.
    expect(authServiceMock.login).not.toHaveBeenCalled();
    // The message is also actually displayed on screen.
    expect(fixture.nativeElement.querySelector('p').textContent).toContain(
      'Please enter a username',
    );
  });

  // B2: click with a username → authService.login is called with that username.
  it('should call authService.login with the username when clicking with a username', async () => {
    authServiceMock.login.and.resolveTo(true); // login returns a Promise<boolean>
    typeUsername('Alice');

    getButton().click();
    // onLogin() is asynchronous: we wait for pending promises to settle.
    await fixture.whenStable();

    expect(authServiceMock.login).toHaveBeenCalledWith('Alice');
  });

  // ===================================================================
  // Group C — `message` values depending on the login outcome
  // (we drive the async by calling onLogin() directly)
  // ===================================================================

  // C1: login succeeds → redirect to /home, message stays at 'Starting login...'.
  it('should navigate to /home on successful login', async () => {
    component.username = 'Alice';
    authServiceMock.login.and.resolveTo(true);

    await component.onLogin();

    expect(routerMock.navigate).toHaveBeenCalledWith(['/home']);
    expect(component.message).toBe('Starting login...');
  });

  // C2: login fails (false) → "missing token" error message, no navigation.
  it('should set message to "Error: Missing token" when login returns false', async () => {
    component.username = 'Alice';
    authServiceMock.login.and.resolveTo(false);

    await component.onLogin();

    expect(component.message).toBe('Error: Missing token');
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

  // C3: login throws → "Login failed" message + formatted detail, no navigation.
  it('should set a "Login failed" message when login throws', async () => {
    component.username = 'Alice';
    authServiceMock.login.and.rejectWith(new Error('network down'));

    await component.onLogin();

    expect(component.message).toContain('Login failed');
    expect(component.message).toContain('boom'); // what getErrorDetailForUser (mock) returns
    expect(authServiceMock.getErrorDetailForUser).toHaveBeenCalled();
    expect(routerMock.navigate).not.toHaveBeenCalled();
  });

});
