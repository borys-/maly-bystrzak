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
  setPreference: (key, value) => localStorage.setItem(`maly-bystrzak:${key}`, value),
  getSharedConfiguration: () => {
    const encoded = new URLSearchParams(location.search).get('book');
    if (!encoded) return null;
    const base64 = encoded.replace(/-/g, '+').replace(/_/g, '/')
      .padEnd(Math.ceil(encoded.length / 4) * 4, '=');
    const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0));
    return new TextDecoder().decode(bytes);
  },
  copyConfigurationLink: async json => {
    const bytes = new TextEncoder().encode(json);
    let binary = '';
    bytes.forEach(value => binary += String.fromCharCode(value));
    const encoded = btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
    const link = `${location.origin}${location.pathname}?book=${encoded}`;
    try {
      await navigator.clipboard.writeText(link);
      return true;
    } catch {
      const input = document.createElement('textarea');
      input.value = link;
      input.style.position = 'fixed';
      input.style.opacity = '0';
      document.body.appendChild(input);
      input.select();
      const copied = document.execCommand('copy');
      input.remove();
      return copied;
    }
  }
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
