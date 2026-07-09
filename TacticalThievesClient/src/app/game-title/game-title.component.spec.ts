// Outils fournis par Angular pour tester un composant :
// - TestBed : le "banc de test" qui construit une mini-application autour du composant.
// - ComponentFixture : l'enveloppe qui donne accès à l'instance du composant ET à son DOM.
import { ComponentFixture, TestBed } from '@angular/core/testing';
// Le Router : c'est la dépendance qu'on va remplacer par un faux (mock) pour ne pas
// réellement changer de page pendant les tests.
import { Router } from '@angular/router';
// Le composant que l'on teste.
import { GameTitleComponent } from './game-title.component';

// describe(...) regroupe tous les tests de ce composant dans une même "suite".
describe('GameTitleComponent', () => {
  // Variables partagées entre les tests, (re)initialisées avant chacun via beforeEach.
  let component: GameTitleComponent;                 // l'instance du composant
  let fixture: ComponentFixture<GameTitleComponent>; // l'enveloppe (accès instance + DOM)
  let routerMock: { navigate: jasmine.Spy };         // notre faux Router

  // beforeEach s'exécute AVANT chaque test (it), pour repartir d'un état propre à chaque fois.
  beforeEach(async () => {
    // On crée un faux Router : un objet qui a une méthode navigate, mais qui est un "spy".
    // Un spy enregistre chaque appel (avec quels arguments) sans exécuter la vraie logique.
    routerMock = { navigate: jasmine.createSpy('navigate') };

    // On configure le module de test :
    // - imports : le composant est "standalone", donc on l'importe directement.
    // - providers : on dit à Angular "quand quelqu'un demande Router, donne-lui notre mock".
    await TestBed.configureTestingModule({
      imports: [GameTitleComponent],
      providers: [{ provide: Router, useValue: routerMock }],
    }).compileComponents(); // compile le HTML/CSS du composant.

    // On crée concrètement le composant et on récupère son instance.
    fixture = TestBed.createComponent(GameTitleComponent);
    component = fixture.componentInstance;

    // detectChanges() déclenche le rendu Angular (le DOM est construit à partir du template).
    fixture.detectChanges();
  });

  // --- Test 1 : le composant s'instancie sans erreur (smoke test) ---
  it('should create', () => {
    // toBeTruthy() vérifie que component n'est ni null ni undefined.
    expect(component).toBeTruthy();
  });

  // --- Test 2 : le titre est bien affiché dans le DOM rendu ---
  it('should display the game title', () => {
    // nativeElement = l'élément HTML racine du composant. On cherche le titre par son id.
    const titleElement: HTMLElement =
      fixture.nativeElement.querySelector('#title');

    // On vérifie d'abord que l'élément existe...
    expect(titleElement).toBeTruthy();
    // ...puis que son texte contient bien le nom du jeu.
    expect(titleElement.textContent).toContain('Tactical Thieves');
  });

  // --- Test 3 : cliquer sur le bouton "Start" appelle la méthode startGame() ---
  it('should call startGame() when the Start button is clicked', () => {
    // On espionne la méthode startGame du composant pour savoir si elle est appelée.
    spyOn(component, 'startGame');

    // On récupère le bouton dans le DOM.
    const button: HTMLButtonElement =
      fixture.nativeElement.querySelector('button.btn-play');

    // On simule un vrai clic utilisateur.
    button.click();

    // On vérifie que le clic a bien déclenché la méthode (grâce au binding (click) du template).
    expect(component.startGame).toHaveBeenCalled();
  });

  // --- Test 4 : startGame() navigue vers /unity-game (comportement clé) ---
  it('should navigate to /unity-game on startGame()', () => {
    // On appelle directement la méthode du composant.
    component.startGame();

    // On vérifie que le Router (notre mock) a bien reçu l'ordre de naviguer,
    // avec exactement le bon chemin.
    expect(routerMock.navigate).toHaveBeenCalledWith(['/unity-game']);
  });
});
