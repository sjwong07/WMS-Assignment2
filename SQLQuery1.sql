UPDATE m
SET m.PhotoURL = p.PhotoURL
FROM MenuItems m
INNER JOIN (
    SELECT MenuItemId, PhotoURL, ROW_NUMBER() OVER (PARTITION BY MenuItemId ORDER BY Id) as rn
    FROM MenuItemPhotos
) p ON m.Id = p.MenuItemId
WHERE p.rn = 1 AND (m.PhotoURL IS NULL OR m.PhotoURL = '');