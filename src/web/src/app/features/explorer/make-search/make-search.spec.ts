import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { VehicleMake } from '../../../core/vehicle-contracts';
import { highlightMatch, MakeSearch } from './make-search';

describe('highlightMatch', () => {
  it('splits the name around the first match, ignoring case', () => {
    expect(highlightMatch('HON-TECH', 'tech')).toEqual({
      before: 'HON-',
      match: 'TECH',
      after: '',
    });
  });

  it('returns the whole name unmatched when the term is not in it', () => {
    expect(highlightMatch('Honda', 'bmw')).toEqual({ before: 'Honda', match: '', after: '' });
  });
});

describe('MakeSearch', () => {
  it('chooses the option picked with the arrow keys and Enter', async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(MakeSearch);
    const selected: VehicleMake[] = [];
    fixture.componentInstance.makeSelected.subscribe((make) => selected.push(make));
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');

    input.value = 'hon';
    input.dispatchEvent(new Event('input'));
    // Waits out the search debounce (150 ms).
    await new Promise((resolve) => setTimeout(resolve, 200));
    TestBed.tick();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/makes?search=hon&limit=10')
      .flush([
        { id: 474, name: 'HONDA' },
        { id: 10795, name: 'HON-TECH' },
      ]);
    await TestBed.inject(ApplicationRef).whenStable();

    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(selected).toEqual([{ id: 10795, name: 'HON-TECH' }]);
    TestBed.tick();
    expect(input.getAttribute('aria-expanded')).toBe('false');
  });
});
