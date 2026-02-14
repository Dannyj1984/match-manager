import { describe, it, expect } from '@jest/globals'
import { formatDate } from '../../utils/format-helpers';

describe('format-helpers', () => {
    describe('formatDate', () => {
        it('should return an empty string if dateString is null', () => {
            expect(formatDate(null)).toBe('');
        });

        it('should return an empty string if dateString is undefined', () => {
            expect(formatDate(undefined)).toBe('');
        });

        it('should return an empty string if dateString is empty', () => {
            expect(formatDate('')).toBe('');
        });

        it('should correctly format a date string', () => {
            // Using a fixed date to avoid timezone issues in comparison
            // Note: Intl format might vary depending on environment locale, 
            // but for en-GB it should be predictable.
            const date = '2023-10-27';
            const formatted = formatDate(date);
            expect(formatted).toContain('Oct');
            expect(formatted).toContain('2023');
            expect(formatted).toContain('27');
        });
    });
});
