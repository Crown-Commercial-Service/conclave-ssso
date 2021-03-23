import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ciiService } from './cii.service';

class HttpClientMock {
    public get() {
      return 'response';
    }
}

describe('ciiService', () => {
    
  let service: ciiService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ ],
      providers: [
        { provide: HttpClient, useClass: HttpClientMock },
      ]
    });
    service = TestBed.inject(ciiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

});
