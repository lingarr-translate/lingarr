/**
 * Creates a promise that resolves after a specified delay
 * @param ms - The delay duration in milliseconds
 */
export const delay = (ms: number): Promise<void> => {
    return new Promise((resolve) => setTimeout(resolve, ms))
}
