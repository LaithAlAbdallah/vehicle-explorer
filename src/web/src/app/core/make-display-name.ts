/**
 * NHTSA stores make names in upper case ("MERCEDES-BENZ"). The popular makes get their usual spelling;
 * every other make is shown exactly as NHTSA stores it, because a rule like "capitalise each word"
 * would break names such as "BMW" or "GMC".
 */
const DisplayNames: Readonly<Record<string, string>> = {
  AUDI: 'Audi',
  CHEVROLET: 'Chevrolet',
  DODGE: 'Dodge',
  FORD: 'Ford',
  HONDA: 'Honda',
  HYUNDAI: 'Hyundai',
  JEEP: 'Jeep',
  KIA: 'Kia',
  LEXUS: 'Lexus',
  MAZDA: 'Mazda',
  'MERCEDES-BENZ': 'Mercedes-Benz',
  NISSAN: 'Nissan',
  PORSCHE: 'Porsche',
  SUBARU: 'Subaru',
  TESLA: 'Tesla',
  TOYOTA: 'Toyota',
  VOLKSWAGEN: 'Volkswagen',
  VOLVO: 'Volvo',
};

export function toMakeDisplayName(name: string): string {
  return DisplayNames[name] ?? name;
}
