import type { Metadata } from 'next';
import { Outfit } from 'next/font/google';
import '@/styles/global.scss';
import { AuthProvider } from '@/contexts/AuthContext';

const outfit = Outfit({ subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'Fictional Company',
  description: 'Employee Management System',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <body className={outfit.className}>
        <AuthProvider>
          {children}
        </AuthProvider>
      </body>
    </html>
  );
}
