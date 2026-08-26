import { Badge } from '@/components/ui/badge';

export function BrandStatusBadge({ active, label }: { active: boolean; label: string }) {
  return (
    <Badge variant={active ? 'default' : 'secondary'} className="whitespace-nowrap">
      {label}
    </Badge>
  );
}
