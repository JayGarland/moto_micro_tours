import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSliderModule } from '@angular/material/slider';
import { TourFilters } from '../../../core/models/tour.models';

@Component({
  selector: 'app-tour-filter',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatIconModule,
    MatSliderModule
  ],
  templateUrl: './tour-filter.component.html',
  styleUrls: ['./tour-filter.component.scss']
})
export class TourFilterComponent {
  @Output() filterChange = new EventEmitter<TourFilters>();

  filterForm: FormGroup;
  maxPriceValue = 50;

  constructor(private fb: FormBuilder) {
    this.filterForm = this.fb.group({
      startDate: [null],
      endDate: [null],
      area: [''],
      priceMax: [50]
    });
  }

  onSearch(): void {
    const formValue = this.filterForm.value;
    const filters: TourFilters = {};

    // Format start/end dates as yyyy-MM-dd if provided
    if (formValue.startDate) {
      const sd = new Date(formValue.startDate);
      filters.startDate = sd.toISOString().split('T')[0];
    }
    if (formValue.endDate) {
      const ed = new Date(formValue.endDate);
      filters.endDate = ed.toISOString().split('T')[0];
    }

    if (formValue.area?.trim()) {
      filters.area = formValue.area.trim();
    }

    if (formValue.priceMax) {
      filters.priceMax = formValue.priceMax;
    }

    this.filterChange.emit(filters);
  }

  onClear(): void {
    this.filterForm.reset({
      startDate: null,
      endDate: null,
      area: '',
      priceMax: 50
    });
    this.maxPriceValue = 50;
    this.filterChange.emit({});
  }

  onPriceChange(event: any): void {
    this.maxPriceValue = event.target?.value || this.filterForm.get('priceMax')?.value || 50;
  }
}
