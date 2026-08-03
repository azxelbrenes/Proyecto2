import { TestBed } from '@angular/core/testing';

import { Tienda } from './tienda';

describe('Tienda', () => {
  let service: Tienda;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(Tienda);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
