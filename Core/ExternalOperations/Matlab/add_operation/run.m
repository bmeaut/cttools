function ctx = run()
%% read context
fid = fopen('op_context.json'); 
context = jsondecode(char(fread(fid,inf)'));
fclose(fid); 

%% modify context
context.BlobImages(context.ActiveLayer).BlobImage(1:100, 1:100) = 6;
context.BlobImages(context.ActiveLayer).Tags.x6(1, :).Name = 'matlab_test_1';
context.BlobImages(context.ActiveLayer).Tags.x6(1, :).Value = 66;
context.BlobImages(context.ActiveLayer).Tags.x6(2, :).Name = 'matlab_test_2';
context.BlobImages(context.ActiveLayer).Tags.x6(2, :).Value = 6666;

context.BlobImages(context.ActiveLayer).BlobImage(150:200, 150:200) = 8;
context.BlobImages(context.ActiveLayer).Tags.x8(1, :).Name = 'matlab_test_1';
context.BlobImages(context.ActiveLayer).Tags.x8(1, :).Value = 88;
context.BlobImages(context.ActiveLayer).Tags.x8(2, :).Name = 'matlab_test_2';
context.BlobImages(context.ActiveLayer).Tags.x8(2, :).Value = 8888;

%% return with JSON encoded context
ctx = jsonencode(context, 'PrettyPrint', true);
%% write context
% fid = fopen('op_context_out.json', 'w');
% fprintf(fid, jsonencode(context, 'PrettyPrint', true));
% fclose(fid);
end

