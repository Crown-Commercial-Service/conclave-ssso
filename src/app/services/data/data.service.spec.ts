import { HttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { dataService } from './data.service';

class HttpClientMock {
    public get() {
      return 'response';
    }
}

describe('dataService', () => {
    
    let service: dataService;

    beforeEach(() => {
        TestBed.configureTestingModule({
            imports: [ ],
            providers: [
              { provide: HttpClient, useClass: HttpClientMock },
            ]
        });
        service = TestBed.inject(dataService);
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

});