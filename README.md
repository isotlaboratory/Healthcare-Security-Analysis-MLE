# Introduction #

Precision medicine is an emerging approach for disease treatment and prevention that delivers
personalized care to individual patients by considering their genetic makeups, medical histories,
environments, and lifestyles. Despite the rapid advancement of precision medicine and its
considerable promise, several underlying technological challenges remain unsolved. One such
challenge of great importance is the security and privacy of precision health–related data, such as
genomic data and electronic health records, which stifle collaboration and hamper the full potential
of machine-learning (ML) algorithms. To preserve data privacy while providing ML solutions, in our
article, [Briguglio, et. al.](https://arxiv.org/abs/2102.03412), we provide three contributions.
First, we propose a generic machine learning with encryption (MLE) framework, which we used to build
an ML model that predicts cancer from one of the most recent comprehensive genomics datasets in the
field. Second, our framework’s prediction accuracy is slightly higher than that of the most recent
studies conducted on the same dataset, yet it maintains the privacy of the patients’ genomic data.
Third, to facilitate the validation, reproduction, and extension of this work, we provide an
open-source repository that contains:

* the design and implementation of the MLE framework (folder
  [`SystemArchitecture`](./SystemArchitecture)). Please, read below for more information.
* all the ML experiments and code (folder [`ModelTraining`](./ModelTraining))
* the final predictive model deployed and the MLE framework, both deployed to a free cloud service
  [`http://mle.isot.ca`](http://mle.isot.ca)

# System Architecture of MLE Framework #
The server is meant to be deployed as a service, hence referred to as MLE.service, which is not
exposed to the network. Instead, nginx (or something similar) should be used as a reverse proxy
which manages incoming HTTP traffic and forwards the appropriate HTTP traffic to the MLE service.
The nginx reverse proxy is also deployed as a service, nginx.service. Below is a summary for
maintenance after deploying on a Ubuntu machine with nginx reverse proxy. Instructions for
deployment can be found
[here](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-5.0).

## Running and configuring the services/proxy ##

You can stop or restart a service, or check a service's status (while it is running or stopped) using:

    sudo systemctl [restart|stop|status] [MLE|nginx]

The MLE service can be configured by editing `/etc/systemd/system/MLE.service`. After making edits,
the service will have to be reloaded with:

    systemctl daemon-reload

An exmple .NET service configuration can be found [here](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-5.0#create-the-service-file). The `ExecStart` line should read:

    ExecStart=/usr/bin/dotnet /var/www/MLE/CDTS_PROJECT.dll

The `nginx` rules for the MLE service can be configured by editing
`/etc/nginx/sites-available/default`. The global `nginx` rules, for all services, can be configured
by editing `/etc/nginx/nginx.conf`. Likewise, after making edits run:

    sudo nginx -t

Which verifies the syntax of the configuration files, and

    sudo nginx -s reload

An exmple nginx service configuration can be found [here](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-5.0#configure-nginx).

## Redeploying after making code changes ##

`/Server` contains the server side applications

From the Server directory, do the following:

* compile with:

        dotnet publish --configuration Release

* After compilation, copy the entire publishing folder into `/var/www/MLE` with:

        sudo rm /var/www/MLE/ -r
        sudo cp ./bin/Release/netcoreapp3.1/publish/ /var/www/MLE/ -r

* Then restart the service with:

        sudo systemctl restart MLE.service


`/Client` contains the client application

From the Client directory, do the following:

* clone the repo to a Windows machine with .NET install 3.1 installed, compile with:

        dotnet publish --configuration Release -r win-x64 -p:PublishSingleFile=true --self-contained true

* The compiled `exe` will be named `CDTS_Project.exe` in
  `./Client/bin/release/netcoreapp3.1/win-x64/publish/`; rename it `MLE.txt` and copy to both
  `/home/[username]/Healthcare-Security-Analysis/Server/wwwroot/DownloadableFiles/` and
  `/var/www/MLE/wwwroot/DownloadableFiles/`; overwrite the old files if needed with:

        cp /home/[username]/Healthcare-Security-Analysis/Client/bin/Release/netcoreapp3.1/win-x64/publish/CDTS_PROJECT.exe /home/[username]/Healthcare-Security-Analysis/Server/wwwroot/DownloadableFiles/MLE.txt

        cp /home/[username]/Healthcare-Security-Analysis/Client/bin/Release/netcoreapp3.1/win-x64/publish/CDTS_PROJECT.exe /var/www/MLE/wwwroot/DownloadableFiles/MLE.txt

* Then restart the service with:

        sudo systemctl restart MLE.service

# References #
* Briguglio, W., Moghaddam, P., Yousef, W. A., Traore, I., & Mamun, M. (2021) "Machine Learning in
Precision Medicine to Preserve Privacy via Encryption". arXiv Preprint, arXiv:2102.03412.

# Citation #

Please, cite this work as

```
@Misc{Briguglio2021MachineLearningViaEncryption,
  author =       {William Briguglio and Parisa Moghaddam and Waleed A. Yousef and Issa Traore and
                  Mamum Mamun},
  title =        {Machine Learning via Encryption (MLE) Framework for Precision Medicine with
                  Preserving Privacy},
  howpublished = {\url{https://github.com/isotlaboratory/Healthcare-Security-Analysis-MLE}},
  year =         2021
}
```

