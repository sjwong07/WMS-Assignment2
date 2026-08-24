// Initiate GET request (AJAX-supported)
$(document).on('click', '[data-get]', e => {
    e.preventDefault();
    const url = e.target.dataset.get;
    location = url || location;
});

//Trim input
$('[data-trim]').on('change', e => {
    e.target.value = e.target.value.trim();
});


document.addEventListener("DOMContentLoaded", function () {

    const message = document.getElementById("tempDataMessage");

    if (message && message.value) {
        showPopup(message.value);
    }

});

function showPopup(message) {

    const overlay = document.createElement("div");
    overlay.className = "popup-overlay";

    const popup = document.createElement("div");
    popup.className = "popup-box";

    const title = document.createElement("h3");
    title.textContent = "Password Reset";

    const text = document.createElement("p");
    text.textContent = message;

    const button = document.createElement("button");
    button.textContent = "OK";

    button.addEventListener("click", function () {
        overlay.remove();
    });

    popup.appendChild(title);
    popup.appendChild(text);
    popup.appendChild(button);

    overlay.appendChild(popup);

    document.body.appendChild(overlay);
}