// import { TestBed } from '@angular/core/testing';
// import { RouterTestingModule } from '@angular/router/testing';
// import { MatToolbarModule } from '@angular/material/toolbar';
// import { MatSlideToggleModule } from '@angular/material/slide-toggle';
// import { MatButtonModule } from '@angular/material/button';
// import { MatCardModule } from '@angular/material/card';
// import { MatIconModule } from '@angular/material/icon';
// import { MatDialogModule } from '@angular/material/dialog';
// import { MatFormFieldModule } from '@angular/material/form-field';
// import { MatCheckboxModule } from '@angular/material/checkbox';
// import { MatInputModule } from '@angular/material/input';
// import { MatExpansionModule } from '@angular/material/expansion';
// import { FlexLayoutModule } from '@angular/flex-layout';
// import { NzButtonModule } from 'ng-zorro-antd/button';
// import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
// import { NzMenuModule } from 'ng-zorro-antd/menu';
// import { NzBreadCrumbModule } from 'ng-zorro-antd/breadcrumb';
// import { NzSliderModule } from 'ng-zorro-antd/slider';
// import { NzSwitchModule } from 'ng-zorro-antd/switch';
// import { NzLayoutModule } from 'ng-zorro-antd/layout';
// import { ComponentsModule } from './components/index';
// import { AppComponent } from './app.component';
// import { HttpClient } from '@angular/common/http';
// import { TranslateHttpLoader } from '@ngx-translate/http-loader';
// import { TranslateModule, TranslateLoader } from '@ngx-translate/core';
// import { StoreModule } from '@ngrx/store';
// import * as reducers from './store/ui.reducers';

// export function HttpLoaderFactory(http: HttpClient) {
//   return new TranslateHttpLoader(http);
// }

// export function createTranslateLoader(http: HttpClient) {
//   return new TranslateHttpLoader(http, '../assets/i18n/', '.json');
// }

// class HttpClientMock {
//   public get() {
//     return 'response';
//   }
// }

// describe('AppComponent', () => {
//   beforeEach(async () => {
//     await TestBed.configureTestingModule({
//       imports: [
//         RouterTestingModule,
//         FlexLayoutModule,
//         MatToolbarModule,
//         MatSlideToggleModule,
//         MatButtonModule,
//         MatCardModule,
//         MatIconModule,
//         MatDialogModule,
//         MatFormFieldModule,
//         MatInputModule,
//         MatCheckboxModule,
//         MatExpansionModule,
//         NzButtonModule,
//         NzDropDownModule,
//         NzMenuModule,
//         NzBreadCrumbModule,
//         NzSliderModule,
//         NzSwitchModule,
//         NzLayoutModule,
//         ComponentsModule,
//         TranslateModule.forRoot({
//           loader: {
//             provide: TranslateLoader,
//             useFactory: (createTranslateLoader),
//             deps: [HttpClient]
//           }
//         }),
//         StoreModule.forRoot({}),
//         StoreModule.forFeature('ui-state', reducers.reducer),
//       ],
//       declarations: [
//         AppComponent
//       ],
//       providers: [
//         { provide: HttpClient, useClass: HttpClientMock },
//       ]
//     }).compileComponents();
//   });

//   it('should create the app', () => {
//     const fixture = TestBed.createComponent(AppComponent);
//     const app = fixture.componentInstance;
//     expect(app).toBeTruthy();
//   });

//   it(`should have as title 'ccs-sso-web'`, () => {
//     const fixture = TestBed.createComponent(AppComponent);
//     const app = fixture.componentInstance;
//     expect(app.title).toEqual('ccs-sso-web');
//   });

//   it('should render title', () => {
//     const fixture = TestBed.createComponent(AppComponent);
//     fixture.detectChanges();
//     const compiled = fixture.nativeElement;
//     expect(compiled.querySelector('.content span').textContent).toContain('ccs-sso-web app is running!');
//   });
// });
