import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { identityService } from './identity.service';

class HttpClientMock {
    public get() {
      return 'response';
    }
}

describe('identityService', () => {
    
    let service: identityService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [ ],
            providers: [
              { provide: HttpClient, useClass: HttpClientMock },
            ]
        });
        service = TestBed.inject(identityService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

});