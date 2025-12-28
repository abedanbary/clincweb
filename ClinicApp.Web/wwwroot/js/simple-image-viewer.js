/**
 * Simple Image Viewer with Double-Click Fullscreen
 * Double click any image to open in fullscreen
 * Use mouse wheel to zoom
 * Click outside image to close
 */

(function() {
    'use strict';

    // Create fullscreen overlay (only once)
    let overlay = document.getElementById('imageViewerOverlay');
    if (!overlay) {
        overlay = document.createElement('div');
        overlay.id = 'imageViewerOverlay';
        overlay.innerHTML = `
            <style>
                #imageViewerOverlay {
                    display: none;
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    background: rgba(0, 0, 0, 0.95);
                    z-index: 99999;
                    cursor: zoom-out;
                    align-items: center;
                    justify-content: center;
                }
                #imageViewerOverlay.active {
                    display: flex;
                }
                #imageViewerOverlay img {
                    max-width: 90%;
                    max-height: 90%;
                    object-fit: contain;
                    transition: transform 0.3s ease;
                    cursor: grab;
                }
                #imageViewerOverlay img:active {
                    cursor: grabbing;
                }
                #imageViewerOverlay .zoom-indicator {
                    position: fixed;
                    bottom: 30px;
                    left: 50%;
                    transform: translateX(-50%);
                    background: rgba(255, 255, 255, 0.9);
                    color: #333;
                    padding: 10px 20px;
                    border-radius: 25px;
                    font-size: 16px;
                    font-weight: bold;
                    pointer-events: none;
                    box-shadow: 0 2px 10px rgba(0,0,0,0.3);
                }
                #imageViewerOverlay .close-hint {
                    position: fixed;
                    top: 30px;
                    right: 30px;
                    background: rgba(255, 255, 255, 0.9);
                    color: #333;
                    padding: 10px 20px;
                    border-radius: 25px;
                    font-size: 14px;
                    pointer-events: none;
                }
            </style>
            <img src="" alt="Fullscreen Image" />
            <div class="zoom-indicator">100%</div>
            <div class="close-hint">اضغط خارج الصورة للإغلاق</div>
        `;
        document.body.appendChild(overlay);
    }

    const overlayImg = overlay.querySelector('img');
    const zoomIndicator = overlay.querySelector('.zoom-indicator');
    let currentZoom = 1;
    let isDragging = false;
    let startX, startY;
    let translateX = 0, translateY = 0;

    // Function to open image in fullscreen
    function openFullscreen(imgElement) {
        overlayImg.src = imgElement.src;
        overlay.classList.add('active');
        currentZoom = 1;
        translateX = 0;
        translateY = 0;
        updateImageTransform();
        updateZoomIndicator();
    }

    // Function to close fullscreen
    function closeFullscreen() {
        overlay.classList.remove('active');
        currentZoom = 1;
        translateX = 0;
        translateY = 0;
    }

    // Update image transform
    function updateImageTransform() {
        overlayImg.style.transform = `scale(${currentZoom}) translate(${translateX}px, ${translateY}px)`;
    }

    // Update zoom indicator
    function updateZoomIndicator() {
        zoomIndicator.textContent = Math.round(currentZoom * 100) + '%';
    }

    // Close when clicking on overlay background (not on image)
    overlay.addEventListener('click', function(e) {
        if (e.target === overlay) {
            closeFullscreen();
        }
    });

    // Prevent closing when clicking on image
    overlayImg.addEventListener('click', function(e) {
        e.stopPropagation();
    });

    // Mouse wheel zoom
    overlay.addEventListener('wheel', function(e) {
        e.preventDefault();
        
        const zoomSpeed = 0.1;
        const delta = e.deltaY > 0 ? -zoomSpeed : zoomSpeed;
        
        currentZoom += delta;
        
        // Limit zoom range
        if (currentZoom < 0.5) currentZoom = 0.5;
        if (currentZoom > 5) currentZoom = 5;
        
        updateImageTransform();
        updateZoomIndicator();
    });

    // Drag to pan when zoomed
    overlayImg.addEventListener('mousedown', function(e) {
        if (currentZoom > 1) {
            isDragging = true;
            startX = e.clientX - translateX;
            startY = e.clientY - translateY;
            overlayImg.style.cursor = 'grabbing';
            e.preventDefault();
        }
    });

    document.addEventListener('mousemove', function(e) {
        if (isDragging) {
            translateX = e.clientX - startX;
            translateY = e.clientY - startY;
            updateImageTransform();
        }
    });

    document.addEventListener('mouseup', function() {
        if (isDragging) {
            isDragging = false;
            overlayImg.style.cursor = 'grab';
        }
    });

    // ESC key to close
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && overlay.classList.contains('active')) {
            closeFullscreen();
        }
    });

    // Add double-click listener to all images on the page
    function attachToImages() {
        const images = document.querySelectorAll('img');
        images.forEach(img => {
            // Skip if already attached
            if (img.dataset.viewerAttached) return;
            
            img.addEventListener('dblclick', function() {
                openFullscreen(this);
            });
            
            // Add hover effect to show it's clickable
            img.style.cursor = 'pointer';
            img.dataset.viewerAttached = 'true';
        });
    }

    // Attach to existing images
    attachToImages();

    // Watch for new images added dynamically
    const observer = new MutationObserver(function() {
        attachToImages();
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    // Expose function globally for manual use
    window.openImageViewer = function(imgElement) {
        openFullscreen(imgElement);
    };

})();