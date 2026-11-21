import { toMakeDisplayName } from './make-display-name';

describe('toMakeDisplayName', () => {
  it('uses the usual spelling for a popular make', () => {
    expect(toMakeDisplayName('MERCEDES-BENZ')).toBe('Mercedes-Benz');
  });

  it('keeps any other make as NHTSA stores it', () => {
    expect(toMakeDisplayName('BMW')).toBe('BMW');
    expect(toMakeDisplayName('HON-TECH')).toBe('HON-TECH');
  });
});
