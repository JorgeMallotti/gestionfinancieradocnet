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

export type UserRole = 'Admin' | 'Finance' | 'User';

export interface RegisterDto {
  companyName: string;
  fullName: string;
  email: string;
  password: string;
}

export interface LoginDto {
  email: string;
  password: string;
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

// ── Categories ──────────────────────────────────────────────────────────

export interface Category {
  id: string;
  name: string;
  description?: string;
  isDefault: boolean;
}

export interface CreateCategoryDto {
  name: string;
  description?: string;
}

export type UpdateCategoryDto = CreateCategoryDto;

// ── Transactions ────────────────────────────────────────────────────────

export type TransactionType = 'Income' | 'Expense';

export interface Transaction {
  id: string;
  categoryId: string;
  categoryName: string;
  type: TransactionType;
  amount: number;
  currency: string;
  date: string; // ISO DateTimeOffset
  description?: string;
  createdByUserId: string;
  createdAt: string;
}

export interface CreateTransactionDto {
  categoryId: string;
  type: TransactionType;
  amount: number;
  currency: string;
  date: string; // ISO DateTimeOffset
  description?: string;
}

export type UpdateTransactionDto = CreateTransactionDto;

export interface TransactionQuery {
  page?: number;
  pageSize?: number;
  type?: TransactionType;
  categoryId?: string;
  from?: string;
  to?: string;
}

// ── Dashboard ───────────────────────────────────────────────────────────

export interface DashboardSummary {
  totalIncome: number;
  totalExpenses: number;
  balance: number;
  transactionCount: number;
  from?: string;
  to?: string;
}

export interface CategoryBreakdown {
  categoryId: string;
  categoryName: string;
  type: TransactionType;
  amount: number;
  percentage: number;
}

export interface MonthlyPoint {
  year: number;
  month: number;
  income: number;
  expenses: number;
  balance: number;
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
