(function () {
    const STORAGE_KEY = 'clinicLang';
    const RTL_LANGS = ['he', 'ar'];
    const originalTexts = new WeakMap();
    let translations = {};

    function getLang() {
        return localStorage.getItem(STORAGE_KEY) || 'en';
    }

    function applyLang(lang) {
        const dict = translations[lang] || {};
        const isRtl = RTL_LANGS.includes(lang);

        document.documentElement.setAttribute('lang', lang);
        document.documentElement.setAttribute('dir', isRtl ? 'rtl' : 'ltr');

        // Translate all visible text nodes
        const walker = document.createTreeWalker(
            document.body,
            NodeFilter.SHOW_TEXT,
            {
                acceptNode: function (node) {
                    const tag = node.parentElement && node.parentElement.tagName;
                    if (['SCRIPT', 'STYLE', 'INPUT', 'TEXTAREA', 'SELECT'].includes(tag))
                        return NodeFilter.FILTER_REJECT;
                    if (node.textContent.trim())
                        return NodeFilter.FILTER_ACCEPT;
                    return NodeFilter.FILTER_SKIP;
                }
            }
        );

        const nodes = [];
        let n;
        while ((n = walker.nextNode())) nodes.push(n);

        nodes.forEach(function (node) {
            // Save original English text on first encounter
            if (!originalTexts.has(node)) {
                originalTexts.set(node, node.textContent);
            }
            const original = originalTexts.get(node);
            const trimmed = original.trim();
            const translated = dict[trimmed];
            node.textContent = translated
                ? original.replace(trimmed, translated)
                : original;
        });

        // Translate placeholder attributes
        document.querySelectorAll('[placeholder]').forEach(function (el) {
            if (!el._origPlaceholder) el._origPlaceholder = el.placeholder;
            el.placeholder = dict[el._origPlaceholder] || el._origPlaceholder;
        });

        // Update switcher button styles
        document.querySelectorAll('.lang-btn').forEach(function (btn) {
            const active = btn.dataset.lang === lang;
            btn.style.fontWeight = active ? '700' : '400';
            btn.style.background = active ? 'rgba(255,255,255,0.25)' : 'transparent';
            btn.style.borderRadius = active ? '4px' : '4px';
        });

        injectRtlStyles(isRtl);
    }

    function injectRtlStyles(isRtl) {
        let el = document.getElementById('__i18n_rtl_style__');
        if (!el) {
            el = document.createElement('style');
            el.id = '__i18n_rtl_style__';
            document.head.appendChild(el);
        }
        el.textContent = isRtl ? `
            [dir="rtl"] .manager-sidebar { right: 0; left: auto; border-right: none; border-left: 1px solid #e2e8f0; }
            [dir="rtl"] .manager-main   { margin-left: 0; margin-right: 240px; }
            [dir="rtl"] .topbar-right   { flex-direction: row-reverse; }
            [dir="rtl"] .sidebar-link   { text-align: right; }
            [dir="rtl"] .sidebar-section-title { text-align: right; }
            [dir="rtl"] .brand-text     { text-align: right; }
            [dir="rtl"] table           { direction: rtl; }
            [dir="rtl"] th, [dir="rtl"] td { text-align: right; }
            [dir="rtl"] .appointments-filters-section { flex-direction: row-reverse; }
            [dir="rtl"] .modal-header   { flex-direction: row-reverse; }
            [dir="rtl"] .appointments-form-actions { flex-direction: row-reverse; }
        ` : '';
    }

    window.setLanguage = function (lang) {
        localStorage.setItem(STORAGE_KEY, lang);
        applyLang(lang);
    };

    // Load translations then apply
    fetch('/js/translations.json')
        .then(function (r) { return r.json(); })
        .then(function (data) {
            translations = data;
            document.addEventListener('DOMContentLoaded', function () {
                applyLang(getLang());
            });
            // If DOM already ready
            if (document.readyState !== 'loading') {
                applyLang(getLang());
            }
        })
        .catch(function () {
            // Translations failed to load — run in English silently
        });
})();
