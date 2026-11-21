import {
  booleanAttribute,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  linkedSignal,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import { toMakeDisplayName } from '../../../core/make-display-name';
import { VehicleApi } from '../../../core/vehicle-api';
import { VehicleMake } from '../../../core/vehicle-contracts';

const SearchDebounceMilliseconds = 150;

/** The parts of a make name around the first match of the search term, so the match can be highlighted. */
export interface HighlightedName {
  before: string;
  match: string;
  after: string;
}

export function highlightMatch(name: string, term: string): HighlightedName {
  const index = term ? name.toLowerCase().indexOf(term.toLowerCase()) : -1;
  return index < 0
    ? { before: name, match: '', after: '' }
    : {
        before: name.slice(0, index),
        match: name.slice(index, index + term.length),
        after: name.slice(index + term.length),
      };
}

/**
 * Make search as an ARIA combobox (WAI-ARIA pattern "combobox with listbox popup"):
 * type to search, ↑ ↓ to move, Enter to choose, Esc to close, "/" anywhere to focus it.
 */
@Component({
  selector: 'app-make-search',
  templateUrl: './make-search.html',
  styleUrl: './make-search.scss',
  host: { '(document:keydown)': 'focusOnSlash($event)' },
})
export class MakeSearch {
  private readonly vehicleApi = inject(VehicleApi);
  private readonly searchInput = viewChild.required<ElementRef<HTMLInputElement>>('searchInput');

  /** Compact = the small version in the top bar; otherwise the large hero version with a visible label. */
  readonly isCompact = input(false, { transform: booleanAttribute });
  /** The chosen make's name, shown in the field until the user types something else. */
  readonly selectedMakeName = input('');
  readonly makeSelected = output<VehicleMake>();

  protected readonly text = linkedSignal(() => this.selectedMakeName());
  protected readonly isOpen = signal(false);

  /** Waits until typing pauses, so not every keystroke sends a request. */
  protected readonly searchTerm = toSignal(
    toObservable(this.text).pipe(
      debounceTime(SearchDebounceMilliseconds),
      map((text) => text.trim()),
      distinctUntilChanged(),
    ),
    { initialValue: '' },
  );

  /** Searches only while the list is open, so showing a chosen make's name sends no request. */
  protected readonly makes = this.vehicleApi.searchMakes(
    computed(() => (this.isOpen() ? this.searchTerm() : '')),
  );
  /** The found makes; empty on error (reading value() in the error state throws). */
  protected readonly options = computed(() => (this.makes.error() ? [] : this.makes.value()));
  protected readonly isListVisible = computed(() => this.isOpen() && this.searchTerm() !== '');
  /** The highlighted option; back to the first one whenever new results arrive. */
  protected readonly activeIndex = linkedSignal({ source: this.options, computation: () => 0 });
  protected readonly activeOptionId = computed(() => {
    const make = this.options()[this.activeIndex()];
    return this.isListVisible() && make ? this.optionId(make) : null;
  });

  protected readonly toMakeDisplayName = toMakeDisplayName;
  protected readonly highlightMatch = highlightMatch;

  protected onInput(text: string): void {
    this.text.set(text);
    this.isOpen.set(true);
  }

  protected onKeydown(event: KeyboardEvent): void {
    const makes = this.options();
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.isOpen.set(true);
        this.activeIndex.update((index) => Math.max(0, Math.min(index + 1, makes.length - 1)));
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.activeIndex.update((index) => Math.max(index - 1, 0));
        break;
      case 'Enter':
        // Only when the list belongs to what is typed now (not to the text before the last keystrokes).
        const isListCurrent = this.text().trim() === this.searchTerm() && !this.makes.isLoading();
        if (this.isListVisible() && isListCurrent && makes[this.activeIndex()]) {
          event.preventDefault();
          this.select(makes[this.activeIndex()]);
        }
        break;
      case 'Escape':
        this.isOpen.set(false);
        break;
    }
  }

  protected select(make: VehicleMake): void {
    this.text.set(toMakeDisplayName(make.name));
    this.isOpen.set(false);
    this.makeSelected.emit(make);
  }

  protected optionId(make: VehicleMake): string {
    return `make-option-${make.id}`;
  }

  protected focusOnSlash(event: KeyboardEvent): void {
    const isTyping =
      event.target instanceof HTMLInputElement || event.target instanceof HTMLSelectElement;
    if (event.key === '/' && !isTyping) {
      event.preventDefault();
      this.searchInput().nativeElement.focus();
    }
  }
}
