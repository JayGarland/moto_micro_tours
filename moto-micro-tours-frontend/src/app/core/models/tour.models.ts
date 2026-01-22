// Tour models matching backend DTOs

export interface TourListDto {
  id: string;
  title: string;
  startAt: string; // ISO date string
  startLocationText: string;
  endLocationText: string;
  status: string;
  notes?: string;
  organizerUserId?: string; // Phase 1: Explicit tour organizer

  // Aggregated info
  totalCapacity: number;
  totalSeatsBooked: number;
  minPrice: number;
  maxPrice: number;
  motardCount: number;

  motards: TourMotardSummaryDto[];
}

export interface TourMotardSummaryDto {
  tourMotardId: string;
  motardUserId: string;
  motardName: string;
  capacitySeats: number;
  seatsBooked: number;
  pricePerSeat: number;
}

export interface TourDetailDto {
  id: string;
  title: string;
  startAt: string; // ISO date string
  startLocationText: string;
  endLocationText: string;
  status: string;
  notes?: string;
  organizerUserId?: string; // Phase 1: Explicit tour organizer

  motards: TourMotardDetailDto[];
}

export interface TourMotardDetailDto {
  tourMotardId: string;
  motardUserId: string;
  motardName: string;
  motardBio?: string;
  bikeModel?: string;
  avgRating?: number;

  capacitySeats: number;
  seatsBooked: number;
  pricePerSeat: number;
}

export interface CreateTourDto {
  title: string;
  startAt: string; // ISO date string
  startLocationText: string;
  endLocationText: string;
  notes?: string;
  capacitySeats: number;
  pricePerSeat: number;
  publishImmediately: boolean;
}

export interface CreateTourResponseDto {
  tourId: string;
  title: string;
  status: string;
  creatorTourMotardId: string;
  message: string;
}

// Filter interface for tour list
export interface TourFilters {
  // Prefer using startDate/endDate to filter a range. Keep `date` for backward compatibility.
  startDate?: string; // yyyy-MM-dd (inclusive)
  endDate?: string;   // yyyy-MM-dd (inclusive)
  date?: string; // DEPRECATED: Single date filter (yyyy-MM-dd)
  area?: string; // Location search
  priceMax?: number; // Maximum price per seat
  status?: string;
}
