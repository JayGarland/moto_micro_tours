import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { BookingStatus } from '../../models/booking.model';

interface TimelineStep {
  label: string;
  icon: string;
  date?: string;
  state: 'completed' | 'current' | 'upcoming';
}

@Component({
  selector: 'app-booking-status-timeline',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './booking-status-timeline.html',
  styleUrl: './booking-status-timeline.scss',
})
export class BookingStatusTimeline {
  @Input() status!: BookingStatus;
  @Input() createdAt?: string;
  @Input() approvedAt?: string;
  @Input() paidAt?: string;
  @Input() cancelledAt?: string;
  @Input() completedByParticipantAtUtc?: string;
  @Input() completedByMotardAtUtc?: string;
  @Input() completedAtUtc?: string;

  get steps(): TimelineStep[] {
    const steps: TimelineStep[] = [];

    // Step 1: Request sent (always present)
    steps.push({
      label: 'Request Sent',
      icon: 'send',
      date: this.createdAt,
      state: 'completed'
    });

    // Check if cancelled
    if (this.cancelledAt ||
        this.status === BookingStatus.Cancelled ||
        this.status === BookingStatus.CancelledExpired ||
        this.status === BookingStatus.Declined) {
      steps.push({
        label: this.getCancelledLabel(),
        icon: 'cancel',
        date: this.cancelledAt,
        state: 'current'
      });
      return steps;
    }

    // Step 2: Approved by motard
    if (this.approvedAt) {
      steps.push({
        label: 'Approved by Motard',
        icon: 'check_circle',
        date: this.approvedAt,
        state: 'completed'
      });
    } else {
      steps.push({
        label: 'Waiting for Approval',
        icon: 'schedule',
        state: this.status === BookingStatus.Pending ? 'current' : 'upcoming'
      });
    }

    // Step 3: Payment completed
    if (this.paidAt) {
      steps.push({
        label: 'Payment Completed',
        icon: 'payment',
        date: this.paidAt,
        state: 'completed'
      });
    } else if (this.status === BookingStatus.Approved) {
      steps.push({
        label: 'Awaiting Payment',
        icon: 'payments',
        state: 'current'
      });
    } else {
      steps.push({
        label: 'Payment Pending',
        icon: 'payments',
        state: 'upcoming'
      });
    }

    // Step 4: Ready for tour (if paid)
    if (this.status === BookingStatus.Paid) {
      steps.push({
        label: 'Ready for Tour',
        icon: 'two_wheeler',
        state: this.completedAtUtc ? 'completed' : 'current'
      });
    }

    // Step 5: Completion (if both confirmed)
    if (this.completedAtUtc) {
      steps.push({
        label: 'Tour Completed',
        icon: 'verified',
        date: this.completedAtUtc,
        state: 'current'
      });
    } else if (this.completedByParticipantAtUtc || this.completedByMotardAtUtc) {
      steps.push({
        label: 'Awaiting Completion Confirmation',
        icon: 'hourglass_empty',
        state: 'current'
      });
    }

    return steps;
  }

  private getCancelledLabel(): string {
    if (this.status === BookingStatus.Declined) {
      return 'Declined by Motard';
    }
    if (this.status === BookingStatus.CancelledExpired) {
      return 'Cancelled (Payment Expired)';
    }
    return 'Cancelled';
  }

  formatDate(dateString?: string): string {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('fr-FR', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      timeZone: 'Europe/Paris'
    });
  }
}
