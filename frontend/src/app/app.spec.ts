import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { App } from './app';

/** Minimal TranslateService stub — the app component only calls init(). */
const translateStub = {
  use: () => ({ subscribe: () => undefined }),
  instant: (key: string) => key,
  currentLang: () => 'en',
  getBrowserLang: () => 'en',
  setDefaultLang: () => undefined,
  addLangs: () => undefined,
};

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [{ provide: TranslateService, useValue: translateStub }],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the router outlet', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });
});
