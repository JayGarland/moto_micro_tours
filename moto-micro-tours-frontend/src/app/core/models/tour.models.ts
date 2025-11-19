// Tour models matching backend DTOs

export interface TourListDto {
  id: string;
  title: string;
  startAt: string; // ISO date string
  startLocationText: string;
  endLocationText: string;
  status: string;
  notes?: string;
  
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
  
  motards: TourMotardDetailDto[];
}

export interface TourMotardDetailDto {
  tourMotardId: string;
  motardUserId: string;
  motardName: string;
  motardBio?: string;
  bikeModel?: string;
  avgRating?: number;
  completedTours: number;
  
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
  date?: string; // Single date filter (yyyy-MM-dd)
  area?: string; // Location search
  priceMax?: number; // Maximum price per seat
  status?: string;
}
