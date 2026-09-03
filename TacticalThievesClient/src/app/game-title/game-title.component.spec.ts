// Angular tools for testing a component:
// - TestBed: the "test bench" that builds a mini-application around the component.
// - ComponentFixture: the wrapper giving access to the component instance AND its DOM.
import { ComponentFixture, TestBed } from '@angular/core/testing';
// The Router: the dependency we replace with a fake (mock) so we don't
// actually change pages during the tests.
import { Router } from '@angular/router';
// The component under test.
import { GameTitleComponent } from './game-title.component';

// describe(...) groups all the tests for this component into a single "suite".
describe('GameTitleComponent', () => {
  // Variables shared across the tests, (re)initialized before each one via beforeEach.
  let component: GameTitleComponent;                 // the component instance
  let fixture: ComponentFixture<GameTitleComponent>; // the wrapper (instance + DOM access)
  let routerMock: { navigate: jasmine.Spy };         // our fake Router

  // beforeEach runs BEFORE each test (it), to start from a clean state every time.
  beforeEach(async () => {
    // We create a fake Router: an object with a navigate method, but as a "spy".
    // A spy records every call (with which arguments) without running the real logic.
    routerMock = { navigate: jasmine.createSpy('navigate') };

    // We configure the test module:
    // - imports: the component is "standalone", so we import it directly.
    // - providers: we tell Angular "when someone asks for Router, give them our mock".
    await TestBed.configureTestingModule({
      imports: [GameTitleComponent],
      providers: [{ provide: Router, useValue: routerMock }],
    }).compileComponents(); // compiles the component's HTML/CSS.

    // We concretely create the component and grab its instance.
    fixture = TestBed.createComponent(GameTitleComponent);
    component = fixture.componentInstance;

    // detectChanges() triggers Angular rendering (the DOM is built from the template).
    fixture.detectChanges();
  });

  // --- Test 1: the component instantiates without error (smoke test) ---
  it('should create', () => {
    // toBeTruthy() checks that component is neither null nor undefined.
    expect(component).toBeTruthy();
  });

  // --- Test 2: the title is properly displayed in the rendered DOM ---
  it('should display the game title', () => {
    // nativeElement = the component's root HTML element. We look up the title by its id.
    const titleElement: HTMLElement =
      fixture.nativeElement.querySelector('#title');

    // First we check that the element exists...
    expect(titleElement).toBeTruthy();
    // ...then that its text does contain the game name.
    expect(titleElement.textContent).toContain('Tactical Thieves');
  });

  // --- Test 3: clicking the "Start" button calls the startGame() method ---
  it('should call startGame() when the Start button is clicked', () => {
    // We spy on the component's startGame method to know whether it is called.
    spyOn(component, 'startGame');

    // We grab the button from the DOM.
    const button: HTMLButtonElement =
      fixture.nativeElement.querySelector('button.btn-play');

    // We simulate a real user click.
    button.click();

    // We check that the click did trigger the method (thanks to the template's (click) binding).
    expect(component.startGame).toHaveBeenCalled();
  });

  // --- Test 4: startGame() navigates to /unity-game (key behavior) ---
  it('should navigate to /unity-game on startGame()', () => {
    // We call the component's method directly.
    component.startGame();

    // We check that the Router (our mock) did receive the order to navigate,
    // with exactly the right path.
    expect(routerMock.navigate).toHaveBeenCalledWith(['/unity-game']);
  });
});
