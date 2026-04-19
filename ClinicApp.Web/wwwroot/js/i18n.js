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

        // Sync the dropdown value
        document.querySelectorAll('.lang-select').forEach(function (sel) {
            sel.value = lang;
        });

        // Only fix what the flex layout cannot auto-handle: the sidebar border side
        var styleEl = document.getElementById('__i18n_rtl_style__');
        if (!styleEl) {
            styleEl = document.createElement('style');
            styleEl.id = '__i18n_rtl_style__';
            document.head.appendChild(styleEl);
        }
        styleEl.textContent = isRtl
            ? '[dir="rtl"] .manager-sidebar { border-right: none; border-left: 1px solid #e2e8f0; }'
            : '';
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
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', function () { applyLang(getLang()); });
            } else {
                applyLang(getLang());
            }
        })
        .catch(function () { /* translations unavailable, stay in English */ });
})();
