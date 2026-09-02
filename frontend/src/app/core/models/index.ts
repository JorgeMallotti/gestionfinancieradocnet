/**
 * Data models mirroring the backend DTOs (camelCase JSON).
 * Keep in sync with GestionFinanciera.Application DTOs.
 */

// ── Auth ────────────────────────────────────────────────────────────────

export interface AuthResponse {
  accessToken: string;
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
  companyId: string;
  companyName: string;
}

/** Bank demo model: Admin = bank operator/mediator, User = client. */
export type UserRole = 'Admin' | 'User';

export type ClientKind = 'Person' | 'Company';

/** Opens a CLIENT account under the single bank (Pending until Admin approves). */
export interface RegisterDto {
  displayName: string;
  kind: ClientKind;
  email: string;
  password: string;
}

export interface LoginDto {
  email: string;
  password: string;
}

/** Public demo account metadata for the one-click quick access (no credentials). */
export interface DemoAccount {
  key: string;
  role: UserRole;
  companyName: string;
  description: string;
}

// ── Pagination ──────────────────────────────────────────────────────────

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PaginationQuery {
  page?: number;
  pageSize?: number;
}

// ── Accounts ────────────────────────────────────────────────────────────

export type AccountStatus = 'Pending' | 'Active' | 'Suspended';

export interface Account {
  id: string;
  ownerUserId: string;
  displayName: string;
  kind: ClientKind;
  status: AccountStatus;
  balance: number;
  currency: string;
  isTreasury: boolean;
  createdAt: string;
}

/** Lightweight reference used when picking a transfer counterparty. */
export interface AccountRef {
  id: string;
  displayName: string;
  isTreasury: boolean;
}

// ── Movements (immutable ledger) ────────────────────────────────────────

export type MovementType =
  'Transfer' | 'LoanDisbursement' | 'LoanRepayment' | 'CorrectiveTransfer' | 'InitialBalance';

export interface Movement {
  id: string;
  type: MovementType;
  fromAccountId: string;
  fromDisplayName: string;
  toAccountId: string;
  toDisplayName: string;
  amount: number;
  currency: string;
  categoryId?: string;
  categoryName?: string;
  description?: string;
  correctsMovementId?: string;
  occurredAt: string;
}

export interface TransferDto {
  toAccountId: string;
  amount: number;
  categoryId?: string;
  description?: string;
}

export interface MovementQuery {
  page?: number;
  pageSize?: number;
  type?: MovementType;
  from?: string;
  to?: string;
}

// ── Categories (Admin-managed catalog, optional tag) ────────────────────

export interface Category {
  id: string;
  name: string;
  description?: string;
}

export interface CreateCategoryDto {
  name: string;
  description?: string;
}

export type UpdateCategoryDto = CreateCategoryDto;

// ── Loans (simple MVP: no interest, no deadlines) ───────────────────────

export type LoanStatus = 'Pending' | 'Approved' | 'Rejected' | 'Repaid';

export interface Loan {
  id: string;
  clientAccountId: string;
  clientDisplayName: string;
  amount: number;
  repaidAmount: number;
  outstandingAmount: number;
  currency: string;
  reason: string;
  status: LoanStatus;
  decidedByUserId?: string;
  decidedAt?: string;
  decisionNote?: string;
  createdAt: string;
}

export interface RequestLoanDto {
  amount: number;
  reason: string;
}

export interface DecideLoanDto {
  approve: boolean;
  note?: string;
}

export interface RepayLoanDto {
  amount: number;
}

// ── Claims (reclamaciones — Admin mediates, both parties consent) ───────

export type ClaimStatus = 'Open' | 'UnderReview' | 'Resolved' | 'Rejected';

export interface Claim {
  id: string;
  movementId: string;
  claimantAccountId: string;
  claimantDisplayName: string;
  reason: string;
  status: ClaimStatus;
  proposedAmount?: number;
  correctiveFromAccountId?: string;
  correctiveToAccountId?: string;
  payerConsented: boolean;
  payeeConsented: boolean;
  resolutionNote?: string;
  resolutionMovementId?: string;
  createdAt: string;
}

export interface OpenClaimDto {
  movementId: string;
  reason: string;
}

export interface ProposeCorrectionDto {
  amount: number;
  fromAccountId: string;
  toAccountId: string;
  note?: string;
}

// ── Dashboard (caller's own account) ────────────────────────────────────

export interface DashboardSummary {
  balance: number;
  totalIncoming: number;
  totalOutgoing: number;
  movementCount: number;
  from?: string;
  to?: string;
}

export interface MonthlyPoint {
  year: number;
  month: number;
  incoming: number;
  outgoing: number;
  net: number;
}

// ── Audit ───────────────────────────────────────────────────────────────

export type AuditAction = 'Create' | 'Update' | 'Delete';

export interface AuditLogEntry {
  id: string;
  userId: string;
  action: AuditAction;
  entity: string;
  entityId: string;
  beforeJson?: string;
  afterJson?: string;
  ipAddress?: string;
  createdAt: string;
}

// ── Reports ─────────────────────────────────────────────────────────────

export interface EmailReportDto {
  email: string;
  from?: string;
  to?: string;
}
