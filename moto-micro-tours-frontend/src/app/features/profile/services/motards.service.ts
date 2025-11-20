import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { 
  MotardProfileDto, 
  CreateReviewDto, 
  ReviewDto,
  MotardProfileParams 
} from '../../../core/models/motard.models';

@Injectable({
  providedIn: 'root'
})
export class MotardsService {

  constructor(private api: ApiService) {}

  /**
   * Get motard profile with optional upcoming tours
   * @param userId - The motard's user ID
   * @param params - Optional parameters (includeUpcomingTours)
   */
  getMotardProfile(userId: string, params?: MotardProfileParams): Observable<MotardProfileDto> {
    return this.api.get<MotardProfileDto>(`motards/${userId}`, params);
  }

  /**
   * Create a review for a motard
   * @param userId - The motard's user ID
   * @param review - Review data (bookingId, rating, comment)
   */
  createReview(userId: string, review: CreateReviewDto): Observable<ReviewDto> {
    return this.api.post<ReviewDto>(`motards/${userId}/reviews`, review);
  }
}
