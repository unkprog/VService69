
$(document).ready(function () {
    $.get("/data/services.json").then(function (services) {
        const icount = (services && services.length ? services.length : 0);
        let htmlServices = '';
        for (let i = 0; i < icount; i++) {
            const service = services[i];
            htmlServices += `<div class="container-services-row">
                                 <span class="number">${i > 8 ? i + 1 : '0' + (i + 1)}</span>
                                 <span class="name">${service.service}</span>
                                 <span class="price">${service.price}</span>
                             </div>`
        }

        $('.container-row > .container-services').html(htmlServices);
    });
});
