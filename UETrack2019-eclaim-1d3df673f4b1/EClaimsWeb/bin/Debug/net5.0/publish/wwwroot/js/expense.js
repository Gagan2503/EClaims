(function ($) {
    'use strict';
    $(function () {
        var Params = [];

        $(document).ready(function () {

            $("#tblClaims").on('click', '#DeleteClaim', function () {
                debugger;
                var x = document.getElementById("tblClaims").rows.length;
                //  alert(x)
                if (x > 2) {
                    var par = $(this).parent().parent(); //tr
                    $(this).parent().parent().remove();
                    $('#tblClaims').find('tr').each(function (i, el) {
                        if (i != 0) {
                            var tds = $(this).find('td');
                            $(this).find('#lblItem').html(i);
                        }
                    });
                }
                Calculation();
            });

            $(document).on('click', '#AddClaim', function () {
                var message = "";
                var table = document.getElementById("tblClaims");
                for (var i = 1; i < 2; i++) {
                    var row = table.rows[i];
                    for (var j = 0; j < row.cells.length; j++) {
                        var cell = row.cells[j];
                        message += "<td>" + cell.innerHTML + "</td>";

                    }
                }
                $("#tblClaims").append('<tr>' + message + '</tr>');

                message = "";
                var DepartmentID = 0;
                $('#tblClaims').find('tr').each(function (i, el) {
                    debugger;
                    var rowcount = table.rows.length;
                    if (i != 0) {
                        var tds = $(this).find('td');
                        $(this).find('#lblItem').html(i);
                    }
                });
                Calculation();
            });
        });

        function isNumber(evt) {
            //alert("number");
            evt = (evt) ? evt : window.event;
            var charCode = (evt.which) ? evt.which : evt.keyCode;
            if (charCode > 31 && (charCode < 47 || charCode > 57) && charCode != 44) {
                return false;
            }
        }

        function ClearData() {

            //alert('clear data');
            //var ClaimType = $('input[name=radioClaimType]:checked').val();
            //alert('Claim Type' + ClaimType);
            return false;
        }

        function Calculation() {
            var GrandTotal = 0;
            var GrandGST = 0;
            debugger;
            $('#tblClaims').find('tr').each(function (i, el) {
                if (i != 0) {
                    var txtSubTot = $(this).find('#txtSubTotal').val();
                    if (txtSubTot == "NaN" || txtSubTot == "") {
                        //  $('#GrandTotal').val(0.00);
                        //$('#txtSubTotal').val(0.00);
                        txtSubTot = 0.00;
                    }
                    var txtGstTot = $(this).find('#txtGST').val();
                    if (txtGstTot == "NaN" || txtGstTot == "") {
                        //  $('#GrandTotal').val(0.00);
                        //$('#txtSubTotal').val(0.00);
                        txtGstTot = 0.00;
                    }
                    GrandTotal = (parseFloat(GrandTotal) + parseFloat(txtSubTot)).toFixed(2);
                    GrandGST = (parseFloat(GrandGST) + parseFloat(txtGstTot)).toFixed(2);
                }
            });

            // var textValue = $('#txtSubTotal').val();

            //if (txtSubTot == "NaN") {
            //    $('#GrandTotal').val(0.00);
            //    $('#txtSubTotal').val(0.00);
            //}

            document.getElementById('txtGrandTotal').value = (parseFloat(GrandTotal)).toFixed(2);
            document.getElementById('txtGrandTotalWithGST').value = (parseFloat(GrandTotal) + parseFloat(GrandGST)).toFixed(2);


        }

        function getdata(param) {
            var res = param;
            if (res == "SendforApproval") {
                //  alert(res);
                var ClaimType = $('input[name=radioClaimType]:checked').val();
                //    alert('Claim Type' + ClaimType);
            }

            var formData = new FormData();
            var dtClaimsArr = new Array();
            dtClaimsArr.length = 0;

            $('#tblClaims').find('tr').each(function (i, el) {
                if (i != 0) {
                    var ClaimType = $('input[name=radioClaimType]:checked').val();
                    //  alert('Claim Type' + ClaimType);
                    var ClaimDate = $(this).find('#dtClaimDate').val();

                    var Description = $(this).find('#txtDescription').val();

                    var ExpenseCategoryID = $(this).find('#ExpenseCategoryID').val();
                    // alert('ExpenseCategoryID ' + ExpenseCategoryID);


                    var SubTotal = $(this).find('#txtSubTotal').val();
                    var GST = $(this).find('#txtGST').val();
                    var dtClaim = {}
                    dtClaim.DateOfJourney = ClaimDate;
                    dtClaim.Description = Description;
                    dtClaim.ExpenseCategoryID = ExpenseCategoryID;

                    dtClaim.Amount = SubTotal;
                    dtClaim.GST = GST;
                    dtClaimsArr.push(dtClaim);
                }

            });

            formData.append('data', JSON.stringify(
                {
                    Company: $("#lblCompany").html(),
                    ClaimType: $('input[name=radioClaimType]:checked').val(),
                    GrandTotal: $("#txtGrandTotal").val(),
                    TotalAmount: $("#txtGrandTotalWithGST").val(),
                    dtClaims: dtClaimsArr
                }
            ));
            if (res == "SendforApproval") {
                alert('Before calling');
                $('#btnsaveupdate').attr('disabled', 'disabled');
                $.ajax({
                    type: "POST",
                    url: "/ExpenseClaim/SaveItemsSG",
                    data: formData,
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    processData: false,
                    contentType: false,
                    success: function (data) {
                        alert('Success');
                        var myID = data.res;
                        var files = $('#fileInput').prop("files");
                        var url = "/ExpenseClaim/UploadECFiles";
                        var formData = new FormData();
                        for (var i = 0; i < files.length; i++) {
                            formData.append("files", files[i]);
                        }
                        if (files.length > 0) {

                            formData.append("FormFile", files);
                            // Adding more keys/values here if need
                            formData.append('Id', myID);
                            jQuery.ajax({
                                type: 'POST',
                                url: url,
                                data: formData,
                                cache: false,
                                contentType: false,
                                processData: false,
                                beforeSend: function (xhr) {
                                    xhr.setRequestHeader("XSRF-TOKEN",
                                        $('input:hidden[name="__RequestVerificationToken"]').val());
                                },
                                success: function (repo) {
                                    if (repo.status == "success") {
                                        alert("File : " + repo.filename + " is uploaded successfully");
                                    }
                                },
                                error: function () {
                                    alert("Error occurs");
                                }
                            });
                        }


                    },
                    failure: function (response) {
                        alert('Failure');
                        $('#result').html(response);
                    }
                });
            }
        }
    });
})(jQuery);