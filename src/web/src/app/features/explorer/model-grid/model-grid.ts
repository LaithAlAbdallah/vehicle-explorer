import { booleanAttribute, Component, input } from '@angular/core';
import { VehicleModel } from '../../../core/vehicle-contracts';

const SkeletonCards = Array.from({ length: 9 }, (_, index) => index);

/** The models as a responsive grid of cards; skeleton cards in the same shape while loading. */
@Component({
  selector: 'app-model-grid',
  templateUrl: './model-grid.html',
  styleUrl: './model-grid.scss',
})
export class ModelGrid {
  readonly models = input<VehicleModel[]>([]);
  readonly isLoading = input(false, { transform: booleanAttribute });

  protected readonly skeletonCards = SkeletonCards;
}
