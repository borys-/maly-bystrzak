window.malyBystrzak = {
  downloadBytes: (name, contentType, bytes) => {
    const blob = new Blob([new Uint8Array(bytes)], { type: contentType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = name;
    anchor.click();
    URL.revokeObjectURL(url);
  },
  getPreference: key => localStorage.getItem(`maly-bystrzak:${key}`),
  setPreference: (key, value) => localStorage.setItem(`maly-bystrzak:${key}`, value)
};

window.malyBystrzakStore = (() => {
  const open = () => new Promise((resolve, reject) => {
    const request = indexedDB.open('maly-bystrzak', 1);
    request.onupgradeneeded = () => {
      const db = request.result;
      if (!db.objectStoreNames.contains('projects')) db.createObjectStore('projects', { keyPath: 'id' });
    };
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
  const transaction = async mode => (await open()).transaction('projects', mode).objectStore('projects');
  const complete = request => new Promise((resolve, reject) => {
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
  return {
    list: async () => {
      const rows = await complete((await transaction('readonly')).getAll());
      const projects = rows.flatMap(row => {
        try {
          const document = JSON.parse(row.document);
          const summary = JSON.parse(row.summary);
          return document.schemaVersion === 5 ? [summary] : [];
        } catch {
          return [];
        }
      });
      return JSON.stringify(projects.sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)));
    },
    get: async id => {
      const row = await complete((await transaction('readonly')).get(id));
      return row?.document ?? null;
    },
    save: async (id, document, summary) => complete((await transaction('readwrite')).put({ id, document, summary })),
    remove: async id => complete((await transaction('readwrite')).delete(id))
  };
})();
