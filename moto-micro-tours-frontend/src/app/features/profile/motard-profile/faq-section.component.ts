import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';

interface FAQItem {
  question: string;
  answer: string;
}

@Component({
  selector: 'app-faq-section',
  standalone: true,
  imports: [CommonModule, MatExpansionModule, MatIconModule],
  templateUrl: './faq-section.component.html',
  styleUrls: ['./faq-section.component.scss']
})
export class FAQSectionComponent {
  faqItems: FAQItem[] = [
    {
      question: 'Do I need a motorcycle license?',
      answer: 'Yes, you must have a valid motorcycle license to participate in our tours. We verify licenses before the tour begins for safety reasons.'
    },
    {
      question: 'Is gear provided?',
      answer: 'We provide helmets and basic safety gear. However, we recommend bringing your own comfortable riding gear, gloves, and boots for the best experience.'
    },
    {
      question: 'What if it rains?',
      answer: 'Tours proceed rain or shine! We monitor weather conditions and will reschedule only in case of severe weather or safety concerns. Rain gear is recommended.'
    },
    {
      question: 'Insurance & Safety',
      answer: 'All participants must have valid motorcycle insurance. We conduct safety briefings before each tour and follow strict safety protocols throughout the ride.'
    },
    {
      question: 'Cancellation Policy',
      answer: 'Cancellations made more than 24 hours before the tour start time are eligible for a full refund. Cancellations within 24 hours may not be refundable.'
    }
  ];
}
