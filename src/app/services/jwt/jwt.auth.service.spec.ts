import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { JwtAuthService } from './jwt.auth.service';

class HttpClientMock {
    public get() {
      return 'response';
    }
}

describe('JwtAuthService', () => {
    
    let service: JwtAuthService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [ ],
            providers: [
              { provide: HttpClient, useClass: HttpClientMock },
            ]
        });
        service = TestBed.inject(JwtAuthService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

});