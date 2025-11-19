import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { TourListDto, TourDetailDto, TourFilters, CreateTourDto, CreateTourResponseDto } from '../../../core/models/tour.models';

@Injectable({
  providedIn: 'root'
})
export class ToursService {
  private readonly endpoint = 'tours';

  constructor(private apiService: ApiService) {}

  /**
   * Get list of tours with optional filters
   */
  getTours(filters?: TourFilters): Observable<TourListDto[]> {
    const params: any = {};
    
    if (filters?.date) {
      params.date = filters.date;
    }
    if (filters?.area) {
      params.area = filters.area;
    }
    if (filters?.priceMax) {
      params.priceMax = filters.priceMax; // Send as number, backend expects decimal
    }
    if (filters?.status) {
      params.status = filters.status;
    }

    return this.apiService.get<TourListDto[]>(this.endpoint, params);
  }

  /**
   * Get detailed tour information by ID
   */
  getTourById(id: string): Observable<TourDetailDto> {
    return this.apiService.get<TourDetailDto>(`${this.endpoint}/${id}`);
  }

  /**
   * Create a new tour (Motard only)
   */
  createTour(tour: CreateTourDto): Observable<CreateTourResponseDto> {
    return this.apiService.post<CreateTourResponseDto>(this.endpoint, tour);
  }
}
