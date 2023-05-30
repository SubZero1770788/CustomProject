import { ChangeDetectionStrategy, Component, Input, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { Message } from 'src/app/_models/message';
import { MessageService } from 'src/app/_services/message.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  selector: 'app-member-messages',
  templateUrl: './member-messages.component.html',
  styleUrls: ['./member-messages.component.css']
})
export class MemberMessagesComponent {
  @ViewChild('messageForm') messageForm?: NgForm;
  @Input() userName? : string;
  messageContent ='';

  constructor(public messageService: MessageService) {
    
  }

  ngOnInit()
  {
  }

  sendMessage()
  {
    if(!this.userName) return;
    this.messageService.sendMessage(this.userName, this.messageContent).then(() => {
      this.messageForm?.reset();
    })
  }
}
