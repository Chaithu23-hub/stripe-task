export interface Product {
  id: number;
  name: string;
  description: string;
  category: string;
  price: number;
  imageUrl: string;
}
export interface CartLine { product: Product; quantity: number }
export interface Customer {
  name: string; email: string; phone: string; address: string;
  city: string; state: string; postalCode: string;
}
export type PaymentStatus = 'Pending' | 'Paid' | 'Expired' | 'Failed';
export interface Order {
  id: string;
  status: PaymentStatus;
  total: number;
  createdAt: string;
  items: { name: string; unitPrice: number; quantity: number }[];
}
