import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { VehicleApi } from './vehicle-api';

describe('VehicleApi', () => {
  let vehicleApi: VehicleApi;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    vehicleApi = TestBed.inject(VehicleApi);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('searchMakes sends the search term and limit', async () => {
    const makes = TestBed.runInInjectionContext(() => vehicleApi.searchMakes(signal('hon')));
    TestBed.tick();

    httpTesting.expectOne('/api/makes?search=hon&limit=10').flush([{ id: 474, name: 'HONDA' }]);
    // Lets the response reach the resource's value signal.
    await TestBed.inject(ApplicationRef).whenStable();

    expect(makes.value()).toEqual([{ id: 474, name: 'HONDA' }]);
  });

  it('searchMakes sends no request while the search is empty', () => {
    const makes = TestBed.runInInjectionContext(() => vehicleApi.searchMakes(signal('')));
    TestBed.tick();

    httpTesting.expectNone(() => true);
    expect(makes.value()).toEqual([]);
  });

  it('getModelYearOverview requests the make and model year', () => {
    TestBed.runInInjectionContext(() => vehicleApi.getModelYearOverview(signal(474), signal(2015)));
    TestBed.tick();

    httpTesting.expectOne('/api/makes/474/model-years/2015').flush({});
  });
});
