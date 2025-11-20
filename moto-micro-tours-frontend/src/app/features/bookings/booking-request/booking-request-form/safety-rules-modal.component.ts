import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-safety-rules-modal',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="safety-rules-modal">
      <h2 mat-dialog-title>
        <mat-icon>health_and_safety</mat-icon>
        Safety Rules & Guidelines
      </h2>

      <mat-dialog-content>
        <div class="rules-content">
          <section class="rule-section">
            <h3><mat-icon>motorcycle</mat-icon> Equipment Requirements</h3>
            <ul>
              <li>Approved motorcycle helmet (required by law)</li>
              <li>Protective riding gear (jacket, gloves, boots)</li>
              <li>Weather-appropriate clothing</li>
              <li>Personal identification and insurance documents</li>
            </ul>
          </section>

          <section class="rule-section">
            <h3><mat-icon>warning</mat-icon> Before the Ride</h3>
            <ul>
              <li>Arrive 15 minutes before departure time</li>
              <li>Ensure you are well-rested and alert</li>
              <li>Inform the motard of any medical conditions</li>
              <li>Check weather conditions and dress accordingly</li>
              <li>Bring water and any personal medications</li>
            </ul>
          </section>

          <section class="rule-section">
            <h3><mat-icon>route</mat-icon> During the Ride</h3>
            <ul>
              <li>Follow the motard's instructions at all times</li>
              <li>Maintain proper riding position and balance</li>
              <li>Hold onto designated handles/grips</li>
              <li>Lean with the motard through turns</li>
              <li>Do not make sudden movements</li>
              <li>Signal if you need to stop or have discomfort</li>
            </ul>
          </section>

          <section class="rule-section">
            <h3><mat-icon>local_hospital</mat-icon> Emergency Procedures</h3>
            <ul>
              <li>Emergency contact will be notified if needed</li>
              <li>Motards carry first aid kits</li>
              <li>Follow motard's instructions in case of emergency</li>
              <li>Emergency services contact: 112 (Europe)</li>
            </ul>
          </section>

          <section class="rule-section">
            <h3><mat-icon>gavel</mat-icon> Legal & Liability</h3>
            <ul>
              <li>Participants ride at their own risk</li>
              <li>Must be 18+ years old (or with guardian consent)</li>
              <li>Insurance coverage is participant's responsibility</li>
              <li>Follow all traffic laws and regulations</li>
              <li>Zero tolerance for alcohol or drug use</li>
            </ul>
          </section>

          <section class="rule-section important">
            <h3><mat-icon>info</mat-icon> Important Notes</h3>
            <ul>
              <li>Tours may be cancelled due to severe weather</li>
              <li>Cancellation policy applies (see terms)</li>
              <li>Photography/video may be taken during tour</li>
              <li>Respect the motard and other participants</li>
            </ul>
          </section>
        </div>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-raised-button color="primary" mat-dialog-close>
          <mat-icon>check</mat-icon>
          I Understand
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [`
    .safety-rules-modal {
      h2 {
        display: flex;
        align-items: center;
        gap: 12px;
        color: var(--yamaha-blue);
        margin: 0;
        padding: 20px;
        border-bottom: 2px solid var(--yamaha-blue);

        mat-icon {
          font-size: 32px;
          width: 32px;
          height: 32px;
        }
      }

      mat-dialog-content {
        padding: 0;
        max-height: 60vh;
      }

      .rules-content {
        padding: 24px;
      }

      .rule-section {
        margin-bottom: 24px;
        padding: 16px;
        background-color: #f8f9fa;
        border-radius: 8px;
        border-left: 4px solid var(--yamaha-blue);

        &.important {
          background-color: #fff3cd;
          border-left-color: #ff9800;
        }

        h3 {
          display: flex;
          align-items: center;
          gap: 8px;
          color: var(--yamaha-blue);
          margin: 0 0 12px 0;
          font-size: 18px;

          mat-icon {
            font-size: 24px;
            width: 24px;
            height: 24px;
          }
        }

        ul {
          margin: 0;
          padding-left: 20px;

          li {
            margin-bottom: 8px;
            line-height: 1.6;
            color: #333;

            &:last-child {
              margin-bottom: 0;
            }
          }
        }
      }

      mat-dialog-actions {
        padding: 16px 24px;
        border-top: 1px solid #e0e0e0;

        button {
          mat-icon {
            margin-right: 4px;
          }
        }
      }
    }
  `]
})
export class SafetyRulesModalComponent {}
