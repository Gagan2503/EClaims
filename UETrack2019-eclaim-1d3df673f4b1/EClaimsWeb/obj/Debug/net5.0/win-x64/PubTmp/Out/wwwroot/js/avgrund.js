(function ($) {
    'use strict';
    var container = document.documentElement;
    function deactivate() {

        document.removeEventListener('keyup', onDocumentKeyUp, false);
        document.removeEventListener('click', onDocumentClick, false);
        document.removeEventListener('touchstart', onDocumentClick, false);

        container.classList.remove('avgrund-active');
        /*     popup.classList.remove('avgrund-popup-animate');*/

    }
    function closeDialog() {
        avgrund.hide();
    }
    //$("show").deactivate()
    //{
    //    container.classList.remove('avgrund-active');
    //};
    $(function () {
        $(show).avgrund({ onLoad: function (element) { alert('onload avgrund'); } });
        $(show).avgrund({ onUnload: function (element) { alert('on Unload avgrund'); } });
        $('#show').avgrund({
            height: 400,
            width: 900,
            holderClass: 'custom',
            showClose: true,
            showCloseText: 'x',
            onBlurContainer: '.container-scroller',
            template: `

<script type="text/javascript">

//var txtAmountFrom= document.getElementById('txtAmountFrom');
//    txtAmountFrom.value = '5859';
//       alert('hello popup');

//var AmountTo= document.getElementById('AmountTo');
//    AmountTo.value = '1234';
      
/* deactivate();*/


    </script>
           <div class="row">
                    <div class="col-md-12 grid-margin stretch-card">
                        <div class="card">
                            <div class="card-body">
                                <h4 class="card-title">Set Verifier / Approver</h4>
                                <div class="row">
                                    <div class="col-12">
                                        <div class="table-responsive">
                                            <input type="hidden" id="txtid" />
                                            <table id="tblOne" class="table">
                                                <thead>
                                                <tr>
												    <td>
													    Verifier
												    </td>
												    <td>
													    Approver
												    </td>
												    <td>
													    Amount From
												    </td>
												    <td>
													     To amount
												    </td>
                                                </tr>
                                                </thead>
                                                <tbody>
                                                <tr>
													<td>
                                                        <select id="ddlVerifier" name="ddlVerifier" aria-labelledby="ddlVerifier"  class="js-example-basic-single w-100">
                                                            <option value="0">Select Verifier</option>
                                                        </select>
													</td>
													<td>
                                                        <select id="ddlApprover" name="ddlApprover" aria-labelledby="ddlApprover" class="js-example-basic-single w-100">
                                                            <option value="0">Select Approver</option>
                                                        </select>
													</td>
													<td>
														<input type="text" name="AmountFrom" id="txtAmountFrom" maxlength="50" class="form-control" aria-labelledby="AmountFrom"
															    onkeypress="return isNumber(event)"  onDrop="return false;" onpaste="return false" Value="0" />

													</td>
													<td>
														<input type="text" name="AmountTo" id="txtAmountTo" maxlength="50" class="form-control" aria-labelledby="AmountTo"
																 onkeypress="return isNumber(event)"   onDrop="return false;" onpaste="return false" Value="1000" />

													</td>
                                                </tr>
                                                </tbody>
                                            </table>
												<button type="submit" class="btn btn-primary mr-2" onclick="UpdateApprover()">Submit</button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
      `
        });
    });

})(jQuery);
