import {
  useEffect,
} from 'react';

/**
 * Sets the browser tab title for the lifetime of a screen.
 *
 * Examples:
 *   "سلامت سیستم · گالری لباس پاتلت"
 *   "ست راحتی بارداری · گالری لباس پاتلت"
 */
export function useDocumentTitle(
  title: string,
  storeName?: string | null,
) {
  useEffect(() => {
    const previous =
      document.title;

    const normalizedTitle =
      title.trim();

    const normalizedStoreName =
      storeName?.trim();

    if (
      normalizedTitle &&
      normalizedStoreName
    ) {
      document.title =
        `${ normalizedTitle } · ${ normalizedStoreName } `;
    } else if (
      normalizedTitle
    ) {
      document.title =
        normalizedTitle;
    } else if (
      normalizedStoreName
    ) {
      document.title =
        normalizedStoreName;
    }

    return () => {
      document.title =
        previous;
    };
  }, [
    title,
    storeName,
  ]);
}

