import { TestBed } from '@angular/core/testing';

import { Partida } from './partida';

describe('Partida', () => {
  let service: Partida;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Partida);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
